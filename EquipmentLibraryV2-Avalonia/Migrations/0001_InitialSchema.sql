CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS public.user_type
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY (INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1),
    type character varying(120) NOT NULL,
    CONSTRAINT user_type_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.users
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY (INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1),
    user_type_id integer DEFAULT 3,
    login character varying(150),
    first_name character varying(150),
    last_name character varying(150),
    middle_name character varying(150),
    password character varying(128),
    is_active boolean DEFAULT true,
    date_joined timestamp with time zone DEFAULT now(),
    CONSTRAINT users_pkey PRIMARY KEY (id),
    CONSTRAINT user_type_id_fkey FOREIGN KEY (user_type_id)
        REFERENCES public.user_type (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID
);

CREATE TABLE IF NOT EXISTS public.equipment_type
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY (INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1),
    type character varying(120) NOT NULL,
    CONSTRAINT equipment_type_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.equipment
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY (INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1),
    title character varying(158) NOT NULL,
    parameters text,
    user_id integer,
    date_create timestamp with time zone DEFAULT now(),
    year_of_manufacture smallint,
    equipment_type_id integer NOT NULL,
    serial_number character varying(150),
    model character varying(255) NOT NULL,
    inv_num character varying(50) NOT NULL,
    CONSTRAINT equipment_pkey PRIMARY KEY (id),
    CONSTRAINT inv_num UNIQUE (inv_num)
        INCLUDE (inv_num),
    CONSTRAINT equipment_type_fkey FOREIGN KEY (equipment_type_id)
        REFERENCES public.equipment_type (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID,
    CONSTRAINT user_id_fkey FOREIGN KEY (user_id)
        REFERENCES public.users (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
        NOT VALID,
    CONSTRAINT year_of_manufacture_check CHECK (year_of_manufacture >= 1900 AND year_of_manufacture <= 2100) NOT VALID
);

CREATE TABLE IF NOT EXISTS public.maintenance_log
(
    id serial NOT NULL,
    equipment_id integer NOT NULL,
    cert_num character varying(150),
    last_check date,
    next_check date,
    status character varying(50),
    location character varying(255),
    responsible character varying(150),
    CONSTRAINT maintenance_log_pkey PRIMARY KEY (id),
    CONSTRAINT equipment_id_fkey FOREIGN KEY (equipment_id)
        REFERENCES public.equipment (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
);

INSERT INTO public.user_type (id, type) OVERRIDING SYSTEM VALUE VALUES
    (1, 'Администратор'),
    (2, 'Инженер'),
    (3, 'Пользователь')
ON CONFLICT (id) DO NOTHING;

INSERT INTO public.equipment_type (id, type) OVERRIDING SYSTEM VALUE VALUES
    (1, 'СИ (Средства измерений)'),
    (2, 'ИО (Испытательное оборудование)')
ON CONFLICT (id) DO NOTHING;