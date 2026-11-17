-- 0003: throttling of failed logins.
-- Application authenticates through public.authenticate() only.
-- Runs as the function owner, so ela_app needs no read access to public.users.password.

CREATE OR REPLACE FUNCTION public.authenticate(
    p_login    text,
    p_password text
)
    RETURNS TABLE
            (
                status              text,
                retry_after_seconds integer,
                user_id             integer,
                user_login          text,
                user_role           integer
            )
    LANGUAGE plpgsql
    SECURITY DEFINER
    SET search_path = public, pg_temp
AS
$$
DECLARE
    v_login      text        := lower(btrim(p_login));
    v_ip         inet        := inet_client_addr();
    v_fails      integer;
    v_recent     integer;
    v_last_fail  timestamptz;
    v_lock_until timestamptz;
    v_lock_min   integer;
    v_user       public.users%ROWTYPE;
BEGIN
    IF v_login = '' OR p_password IS NULL OR p_password = '' THEN
        RETURN QUERY SELECT 'invalid'::text, NULL::integer, NULL::integer, NULL::text, NULL::integer;
        RETURN;
    END IF;

    -- 1. Per-account lockout: 5 failures / 15 min. Duration grows with the 24h failure count.
    SELECT count(*), max(attempted_at)
      INTO v_fails, v_last_fail
      FROM public.login_attempts
     WHERE login = v_login
       AND NOT succeeded
       AND attempted_at > now() - interval '15 minutes';

    IF v_fails >= 5 THEN
        SELECT count(*)
          INTO v_recent
          FROM public.login_attempts
         WHERE login = v_login
           AND NOT succeeded
           AND attempted_at > now() - interval '24 hours';

        v_lock_min   := CASE WHEN v_recent >= 15 THEN 1440 WHEN v_recent >= 10 THEN 60 ELSE 15 END;
        v_lock_until := v_last_fail + make_interval(mins => v_lock_min);

        IF v_lock_until > now() THEN
            RETURN QUERY SELECT 'locked'::text,
                                ceil(extract(epoch FROM v_lock_until - now()))::integer,
                                NULL::integer, NULL::text, NULL::integer;
            RETURN;
        END IF;
    END IF;

    -- 2. Per-IP soft throttle: 20 failures / 5 min -> 60 s pause. Never a long lock (NAT friendly).
    IF v_ip IS NOT NULL THEN
        SELECT count(*)
          INTO v_recent
          FROM public.login_attempts
         WHERE ip = v_ip
           AND NOT succeeded
           AND attempted_at > now() - interval '5 minutes';

        IF v_recent >= 20 THEN
            RETURN QUERY SELECT 'throttled'::text, 60, NULL::integer, NULL::text, NULL::integer;
            RETURN;
        END IF;
    END IF;

    -- 3. Verify credentials.
    SELECT *
      INTO v_user
      FROM public.users
     WHERE login = p_login
       AND password = crypt(p_password, password)
       AND is_active = true;

    IF NOT FOUND THEN
        INSERT INTO public.login_attempts (login, ip, succeeded) VALUES (v_login, v_ip, false);
        RETURN QUERY SELECT 'invalid'::text, NULL::integer, NULL::integer, NULL::text, NULL::integer;
        RETURN;
    END IF;

    -- 4. Success: audit, clear the account's failures, keep the table small.
    INSERT INTO public.login_attempts (login, ip, succeeded) VALUES (v_login, v_ip, true);
    DELETE FROM public.login_attempts WHERE login = v_login AND NOT succeeded;
    DELETE FROM public.login_attempts WHERE attempted_at < now() - interval '90 days';

    RETURN QUERY SELECT 'ok'::text, NULL::integer, v_user.id, v_user.login::text, v_user.user_type_id;
END;
$$;

REVOKE ALL ON FUNCTION public.authenticate(text, text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION public.authenticate(text, text) TO ela_app;
