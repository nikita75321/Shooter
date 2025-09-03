--
-- PostgreSQL database dump
--

-- Dumped from database version 14.18 (Ubuntu 14.18-0ubuntu0.22.04.1)
-- Dumped by pg_dump version 14.18 (Ubuntu 14.18-0ubuntu0.22.04.1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: clan_members; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.clan_members (
    clan_id integer NOT NULL,
    player_name character varying(50) NOT NULL,
    is_leader boolean DEFAULT false NOT NULL,
    player_id character varying
);


ALTER TABLE public.clan_members OWNER TO postgres;

--
-- Name: clans; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.clans (
    clan_id integer NOT NULL,
    clan_name character varying(50) NOT NULL,
    leader_id character varying NOT NULL,
    leader_name character varying(50) NOT NULL,
    need_rating integer DEFAULT 0,
    is_open boolean DEFAULT true,
    player_count integer DEFAULT 1,
    max_players integer DEFAULT 25,
    clan_points integer DEFAULT 0 NOT NULL,
    clan_level integer DEFAULT 0 NOT NULL
);


ALTER TABLE public.clans OWNER TO postgres;

--
-- Name: clans_clan_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.clans_clan_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.clans_clan_id_seq OWNER TO postgres;

--
-- Name: clans_clan_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.clans_clan_id_seq OWNED BY public.clans.clan_id;


--
-- Name: friends; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.friends (
    player_id integer NOT NULL,
    friend_ids integer[] DEFAULT '{}'::integer[] NOT NULL
);


ALTER TABLE public.friends OWNER TO postgres;

--
-- Name: inapps; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.inapps (
    inapp_id integer NOT NULL,
    player_id integer NOT NULL,
    buy_count integer DEFAULT 0 NOT NULL
);


ALTER TABLE public.inapps OWNER TO postgres;

--
-- Name: players; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.players (
    id integer NOT NULL,
    player_name character varying(255) NOT NULL,
    rating integer DEFAULT 0,
    clan_name character varying(255) DEFAULT NULL::character varying,
    clan_points integer DEFAULT 0 NOT NULL,
    platform character varying(20) DEFAULT 'unknown'::character varying,
    money integer DEFAULT 0 NOT NULL,
    donat_money integer DEFAULT 0 NOT NULL,
    open_characters jsonb DEFAULT '{}'::jsonb NOT NULL,
    love_hero character varying(30),
    best_rating integer DEFAULT 0 NOT NULL,
    overral_kill integer DEFAULT 0 NOT NULL,
    match_count integer DEFAULT 0 NOT NULL,
    win_count integer DEFAULT 0 NOT NULL,
    revive_count integer DEFAULT 0 NOT NULL,
    max_damage integer DEFAULT 0 NOT NULL,
    shoot_count integer DEFAULT 0 NOT NULL,
    friends_reward character varying(255) DEFAULT ''::character varying,
    player_id character varying(255),
    hero_card jsonb DEFAULT '{}'::jsonb NOT NULL,
    hero_match integer[] DEFAULT ARRAY[0, 0, 0, 0, 0, 0, 0, 0],
    hero_lvl jsonb DEFAULT '[0, 0, 0]'::jsonb,
    hero_levels jsonb DEFAULT '[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]'::jsonb,
    last_online timestamp without time zone DEFAULT now()
);


ALTER TABLE public.players OWNER TO postgres;

--
-- Name: players_player_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.players_player_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.players_player_id_seq OWNER TO postgres;

--
-- Name: players_player_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.players_player_id_seq OWNED BY public.players.id;


--
-- Name: clans clan_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clans ALTER COLUMN clan_id SET DEFAULT nextval('public.clans_clan_id_seq'::regclass);


--
-- Data for Name: clan_members; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.clan_members (clan_id, player_name, is_leader, player_id) FROM stdin;
7	lool	t	59-32645
8	lool	t	59-32645
9	lool	t	NaN
10	lool	t	NaN
1	Test1	t	52
1	lool	f	NaN
\.


--
-- Data for Name: clans; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.clans (clan_id, clan_name, leader_id, leader_name, need_rating, is_open, player_count, max_players, clan_points, clan_level) FROM stdin;
8	1231	59-32645	lool	0	t	1	25	0	1
9	2222	59-32645	lool	0	t	1	25	0	1
7	1234	59-32645	lool	0	t	1	25	0	1
1	New_clan	52	test_1	0	t	2	25	0	1
10	3333	59-32645	lool	0	t	1	25	0	1
\.


--
-- Data for Name: friends; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.friends (player_id, friend_ids) FROM stdin;
\.


--
-- Data for Name: inapps; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.inapps (inapp_id, player_id, buy_count) FROM stdin;
\.


--
-- Data for Name: players; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.players (id, player_name, rating, clan_name, clan_points, platform, money, donat_money, open_characters, love_hero, best_rating, overral_kill, match_count, win_count, revive_count, max_damage, shoot_count, friends_reward, player_id, hero_card, hero_match, hero_lvl, hero_levels, last_online) FROM stdin;
1	s	0	\N	0	unknown	0	0	{}	\N	0	0	0	0	0	0	0		generated_1	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
46		74	\N	0	unknown	21000	62	{"Ci-J": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Mono": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 1, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	75	1	40	40	0	8	95		46-26982	{"0": 24, "1": 14, "2": 14, "3": 12, "4": 12, "5": 2}	{15,52,7,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-28 10:30:12.674521
51	clan_fix	-2	\N	0	unknown	7000	3020012	{"Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Mono": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	4	1	8	8	0	8	41		51-27543	{"0": 6}	{11,0,0,0,5,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-27 13:09:48.879351
57	ierh	0	New_clan	0	unknown	13400	39	{"0": "{", "1": "\\"", "2": "K", "3": "a", "4": "y", "5": "e", "6": "l", "7": "\\"", "8": ":", "9": "[", "10": "1", "11": ",", "12": "0", "13": ",", "14": "0", "15": ",", "16": "0", "17": ",", "18": "0", "19": ",", "20": "0", "21": ",", "22": "0", "23": ",", "24": "0", "25": ",", "26": "0", "27": "]", "28": "}", "Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Mono": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 0, 0, 1, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	0	0	0	0	0	0	0	0		57-57528	{"0": 14, "1": 16, "2": 11, "3": 7, "4": 4}	{2,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-29 10:20:27.654792
52		26	New_clan	0	unknown	36200	68	{"Coco": [1, 0, 0, 0, 1, 0, 0, 0, 0], "Mono": [1, 0, 0, 1, 0, 0, 0, 0, 0], "Bobby": [1, 0, 1, 0, 0, 0, 0, 0, 0], "Kayel": [1, 1, 0, 0, 0, 1, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 1, 0]}	0	26	0	15	15	0	0	0		52-44743	{"0": 31, "1": 23, "2": 20, "3": 8}	{14,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-28 09:42:00.074893
55	Yatut	125	New_clan	0	unknown	27050	71	{"Ci-J": [1, 1, 0, 0, 1, 0, 0, 0, 0], "Coco": [1, 1, 0, 0, 1, 0, 0, 0, 0], "Mono": [1, 0, 0, 1, 0, 0, 0, 1, 0], "Bobby": [1, 0, 1, 0, 0, 1, 0, 0, 0], "Kayel": [1, 1, 0, 0, 0, 1, 0, 1, 0], "Zetta": [1, 0, 1, 1, 0, 0, 0, 0, 0], "Freddy": [1, 1, 0, 0, 0, 0, 0, 1, 0]}	Kayel	129	3	67	67	0	32	127		55-41232	{"0": 30, "1": 20, "2": 26, "3": 8, "4": 8, "5": 4}	{16,10,0,8,0,0,0,0}	[0, 0, 0]	[{"rank": 2, "level": 11}, {"rank": 2, "level": 11}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-09-01 10:05:05.548437
32	nekto	20	\N	0	unknown	17450	71	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Rambo": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	20	4	1	1	1	20057	5		32-69956	{"0": 15, "7": 20}	{1,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 6, "level": 50}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 2}]	2025-08-20 22:25:16.578965
3	chel	48	\N	0	unknown	2550	10	{}	\N	48	4	8	7	3	29	169		generated_3	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
4	ko	0	\N	0	unknown	0	0	{}	\N	0	0	0	0	0	0	0		generated_4	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
5	ыы	0	\N	0	unknown	0	0	{}	\N	0	0	0	0	0	0	0		generated_5	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
6	фы	0	\N	0	unknown	0	0	{}	\N	0	0	0	0	0	0	0		generated_6	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
7	zz	0	\N	0	unknown	0	0	{"Kayel": 1}	\N	0	0	0	0	0	0	0		generated_7	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
8	test	0	\N	0	unknown	0	0	{"Kayel": 1}	Kayel	0	0	0	0	0	0	0		generated_8	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
10	test3	0	\N	0	unknown	0	0	{"Kayel": 1}	Kayel	0	0	0	0	0	0	0		generated_10	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
11	test4	0	\N	0	unknown	0	0	{"Kayel": 1}	Kayel	0	0	0	0	0	0	0		generated_11	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
12	test5	0	\N	0	unknown	0	0	{"Kayel": 1}	Kayel	0	0	0	0	0	0	0		generated_12	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
13	test6	0	\N	0	unknown	0	0	{"Kayel": 1}	Kayel	0	0	0	0	0	0	0		generated_13	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
14	test7	0	\N	0	unknown	0	0	{"Kayel": 1}	Kayel	0	0	0	0	0	0	0		generated_14	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
29	третий	48	\N	0	unknown	145950	962	{"Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Rambo": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	50	4	5	5	0	16	62		29-23066	{"0": 952}	{0,2,0,0,0,0,0,4}	[0, 0, 0]	[{"rank": 1, "level": 3}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 3}]	2025-08-20 22:25:16.578965
15	test8	0	\N	0	unknown	0	0	{"Kayel": 1}	Kayel	0	0	0	0	0	0	0		generated_15	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
16	test9	0	\N	0	unknown	0	0	{"Kayel": 1}	Kayel	0	0	0	0	0	0	0		generated_16	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
17	test11	20	\N	0	unknown	1000	4	{"Kayel": 1}	Kayel	20	1	1	1	1	24	19		generated_17	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
27	перый	20	\N	0	unknown	1000	4	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	20	0	1	1	0	0	1		27-89804	{}	{1,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
28	второй	60	\N	0	unknown	5550	12	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	60	8	3	3	9	24	66		28-50235	{"0": 5}	{3,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
25	ss	45	\N	0	unknown	4200	43	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	45	3	3	3	1	24	30		25-41433	{"0": 24}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
24	chel	0	\N	0	unknown	1900	38	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		24-98847	{"0": 6, "Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
26	y     y	198	\N	0	unknown	13450	66	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	198	50	31	31	56	70	729		26-60619	{"0": 26}	{24,0,0,0,1,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
9	ttt	0	\N	0	unknown	0	0	{"Kayel": 1}	Kayel	0	0	0	0	0	0	0		generated_9	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
19	z	0	\N	0	unknown	0	0	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		19-36302	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
20	z	0	\N	0	unknown	0	0	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		20-82179	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
21	zzzz	0	\N	0	unknown	0	0	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		21-27639	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
22	z	0	\N	0	unknown	0	0	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		22-70538	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
23	weeew	30	\N	0	unknown	1500	6	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	30	2	3	3	0	24	22		23-39830	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
35	шпек	0	\N	0	unknown	0	0	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		35-95285	{}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
30	aaaaaaaa	5	\N	0	unknown	157950	12945	{"Ci-J": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Mono": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Rambo": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Zetta": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	0	5	0	1	1	0	0	0		30-86398	{"0": 76, "7": 75}	{2,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 6, "level": 50}, {"rank": 6, "level": 50}, {"rank": 6, "level": 50}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 3, "level": 21}]	2025-08-20 22:25:16.578965
31	noviy	23	\N	0	unknown	54200	246	{"Ci-J": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Mono": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Rambo": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Zetta": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	25	1	6	6	0	5	24		31-53099	{"0": 50, "1": 25, "2": 150, "7": 15}	{1,0,0,0,0,0,0,9}	[0, 0, 0]	[{"rank": 1, "level": 3}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
45	test_Skin	107	\N	0	unknown	30250	62	{"Ci-J": [1, 0, 0, 0, 0, 0, 1, 0, 0], "Coco": [1, 0, 1, 0, 0, 1, 0, 0, 0], "Mono": [1, 0, 0, 1, 0, 0, 0, 0, 0], "Bobby": [1, 0, 1, 0, 0, 0, 0, 0, 0], "Kayel": [1, 1, 0, 0, 0, 0, 0, 0, 0], "Rambo": [1, 0, 0, 0, 1, 0, 0, 0, 0], "Zetta": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	107	6	21	21	0	16	169		45-83015	{"0": 37, "1": 17, "2": 15, "3": 10, "4": 17, "5": 3, "6": 1, "7": 1}	{0,5,0,0,0,67,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-26 08:39:24.948165
37	кто-то	0	\N	0	unknown	0	0	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	0	0	0	0	0	0	0	0		37-24321	{}	{1,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
53	test_1	0	\N	0	unknown	0	0	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		53-13607	{}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-28 09:39:22.004148
54		5	New_clan	0	unknown	5150	11	{"0": "{", "1": "\\"", "2": "K", "3": "a", "4": "y", "5": "e", "6": "l", "7": "\\"", "8": ":", "9": "[", "10": "1", "11": ",", "12": "0", "13": ",", "14": "0", "15": ",", "16": "0", "17": ",", "18": "0", "19": ",", "20": "0", "21": ",", "22": "0", "23": ",", "24": "0", "25": ",", "26": "0", "27": "]", "28": "}", "Coco": [1, 0, 0, 0, 1, 0, 0, 0, 0], "Mono": [1, 0, 0, 1, 0, 0, 0, 0, 0], "Bobby": [1, 0, 1, 0, 0, 0, 0, 0, 0], "Kayel": [1, 1, 0, 0, 0, 1, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 1, 0]}	0	5	0	1	1	0	0	0		54-18401	{"0": 10}	{15,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-28 10:05:29.856928
56	TestOnline	62	0	0	unknown	38200	68	{"Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Mono": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 1, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	62	3	60	60	0	113	181		56-91620	{"0": 14, "1": 6, "2": 8}	{6,20,0,0,0,0,2,0}	[0, 0, 0]	[{"rank": 3, "level": 20}, {"rank": 2, "level": 20}, {"rank": 1, "level": 10}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-09-02 13:04:57.07524
58	Ятут	0	\N	0	unknown	4200	16	{"Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		58-43582	{"0": 6}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-09-01 10:11:33.629173
34	Final0.1	30	\N	0	unknown	795050	692109	{"Ci-J": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Coco": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Mono": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Bobby": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Kayel": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Rambo": [1, 1, 1, 1, 1, 1, 1, 1, 0], "Zetta": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Freddy": [1, 1, 1, 1, 1, 1, 1, 1, 1]}	Kayel	30	7	3	3	0	16	118		34-31834	{"0": 599, "1": 514, "2": 440, "3": 317, "4": 290, "5": 154, "6": 116, "7": 3387}	{0,0,0,0,0,0,0,4}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
36	Шпек_финал	0	\N	0	unknown	0	0	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	0	0	0	0	0	0	0	0		36-45501	{}	{1,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
48	fdg	34	\N	0	unknown	17500	48	{"Ci-J": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Mono": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 1, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	34	1	8	8	0	58	10		48-40776	{"0": 24, "1": 10, "2": 10, "3": 4}	{8,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 10}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-09-02 12:47:16.069364
47	lox	1051	\N	0	unknown	27500	63	{"Ci-J": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Mono": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 1, 0, 0, 0, 0, 0, 0, 0], "Zetta": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	1051	0	26	26	0	8	3		47-10526	{"0": 21, "1": 11, "2": 11, "3": 3, "4": 2}	{55,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 10}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-26 23:40:14.337584
49	aboba	10	\N	0	unknown	500	2	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	10	0	2	2	0	0	0		49-61032	{}	{2,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-27 01:34:08.448349
59	lool	90	New_clan	0	unknown	85150	129	{"Ci-J": [1, 0, 0, 0, 1, 0, 0, 1, 0], "Coco": [1, 0, 0, 0, 0, 0, 1, 0, 0], "Mono": [1, 0, 0, 1, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 1, 0, 0, 0, 0, 0], "Kayel": [1, 1, 0, 0, 0, 0, 0, 1, 0], "Rambo": [1, 0, 0, 0, 0, 0, 1, 0, 0], "Zetta": [1, 0, 0, 0, 0, 0, 0, 1, 0], "Freddy": [1, 0, 1, 0, 0, 0, 0, 0, 0]}	Kayel	90	0	25	25	0	0	0		59-32645	{"0": 60, "1": 50, "2": 35, "3": 15, "4": 15, "5": 6, "6": 4}	{11,23,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 7}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-09-02 14:56:44.543017
50	zaza	5	\N	0	unknown	250	10001	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	Kayel	5	0	1	1	0	0	0		50-98700	{}	{1,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-27 10:08:12.54915
18	jmur	0	\N	0	unknown	0	0	{"Kayel": 1}	Kayel	0	0	0	0	0	0	0		generated_18	{"Kayel": 0}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
33	kyk	0	\N	0	unknown	273900	663	{"Ci-J": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Mono": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Rambo": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Zetta": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		33-59130	{"0": 240, "1": 10213, "2": 160, "3": 117, "4": 103, "5": 18, "6": 10, "7": 124}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-20 22:25:16.578965
41	ssaaas	0	\N	0	unknown	0	0	{"Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		41-56273	{}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-22 11:28:41.181291
38	test_room	15	\N	0	unknown	8700	26	{"0": "{\\"Kayel\\":[1,0,0,0,0,0,0,0,0]}", "1": {"Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0]}, "Ci-J": [1, 0, 0, 1, 1, 0, 1, 0, 0], "Coco": [1, 0, 1, 0, 0, 0, 0, 0, 0], "Mono": [1, 0, 0, 0, 1, 1, 0, 0, 0], "Bobby": [1, 1, 0, 1, 0, 0, 0, 0, 0], "Kayel": [1, 1, 0, 0, 1, 0, 0, 0, 0], "Freddy": [1, 0, 0, 1, 0, 1, 1, 0, 0]}	0	15	0	3	3	0	8	4		38-22006	{"0": 13, "1": 3, "2": 3, "3": 2, "4": 4, "5": 2}	{1,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-21 15:26:50.7845
39	ыыы	70	\N	0	unknown	20100	58	{"0": "{", "1": "\\"", "2": "K", "3": "a", "4": "y", "5": "e", "6": "l", "7": "\\"", "8": ":", "9": "[", "10": "1", "11": ",", "12": "0", "13": ",", "14": "0", "15": ",", "16": "0", "17": ",", "18": "0", "19": ",", "20": "0", "21": ",", "22": "0", "23": ",", "24": "0", "25": ",", "26": "0", "27": "]", "28": "}", "Ci-J": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Coco": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Mono": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Bobby": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Kayel": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Zetta": [1, 1, 1, 1, 1, 1, 1, 1, 1], "Freddy": [1, 1, 1, 1, 1, 1, 1, 1, 1]}	Kayel	70	0	8	8	0	8	15		39-37173	{"0": 22, "1": 14, "2": 12, "3": 12, "4": 8}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 3}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-22 09:54:28.442872
40	new	0	\N	0	unknown	4700	10	{"Ci-J": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Coco": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Mono": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Kayel": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		40-97836	{"0": 6}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-22 09:57:07.20044
42	Ss	5	\N	0	unknown	9950	17	{}	0	5	0	1	1	0	0	0		42-16684	{}	{15,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-22 16:19:01.593775
44	g	0	\N	0	unknown	62300	340	{"Ci-J": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Coco": [1, 0, 0, 0, 0, 1, 0, 0, 0], "Mono": [1, 0, 0, 0, 0, 1, 0, 0, 0], "Bobby": [1, 0, 0, 0, 0, 0, 1, 0, 0], "Kayel": [1, 0, 1, 0, 0, 1, 0, 0, 0], "Zetta": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 0, 0, 0, 0]}	нет	0	0	0	0	0	0	0		44-23832	{"0": 18, "1": 18, "2": 6, "3": 6, "4": 4}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-25 00:41:03.274321
43	olo	0	\N	0	unknown	199550	785	{"Ci-J": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Coco": [1, 0, 1, 1, 0, 0, 0, 0, 0], "Mono": [1, 1, 1, 0, 0, 0, 1, 0, 0], "Bobby": [1, 0, 0, 1, 0, 0, 0, 0, 0], "Kayel": [1, 1, 0, 0, 1, 0, 1, 0, 0], "Rambo": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Zetta": [1, 0, 0, 0, 0, 0, 0, 0, 0], "Freddy": [1, 0, 0, 0, 0, 1, 1, 0, 0]}	нет	0	0	0	0	0	0	0		43-32442	{"0": 124, "1": 66, "2": 74, "3": 48, "4": 27, "5": 11}	{0,0,0,0,0,0,0,0}	[0, 0, 0]	[{"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}, {"rank": 1, "level": 1}]	2025-08-25 00:31:14.589282
\.


--
-- Name: clans_clan_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.clans_clan_id_seq', 10, true);


--
-- Name: players_player_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.players_player_id_seq', 59, true);


--
-- Name: clan_members clan_members_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clan_members
    ADD CONSTRAINT clan_members_pkey PRIMARY KEY (clan_id, player_name);


--
-- Name: clans clans_clan_name_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clans
    ADD CONSTRAINT clans_clan_name_key UNIQUE (clan_name);


--
-- Name: clans clans_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clans
    ADD CONSTRAINT clans_pkey PRIMARY KEY (clan_id);


--
-- Name: friends friends_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.friends
    ADD CONSTRAINT friends_pkey PRIMARY KEY (player_id);


--
-- Name: inapps inapps_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.inapps
    ADD CONSTRAINT inapps_pkey PRIMARY KEY (inapp_id, player_id);


--
-- Name: players players_id_unique; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.players
    ADD CONSTRAINT players_id_unique UNIQUE (id);


--
-- Name: players players_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.players
    ADD CONSTRAINT players_pkey PRIMARY KEY (id);


--
-- Name: friends friends_player_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.friends
    ADD CONSTRAINT friends_player_id_fkey FOREIGN KEY (player_id) REFERENCES public.players(id) ON DELETE CASCADE;


--
-- Name: inapps inapps_player_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.inapps
    ADD CONSTRAINT inapps_player_id_fkey FOREIGN KEY (player_id) REFERENCES public.players(id) ON DELETE CASCADE;


--
-- Name: TABLE clan_members; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.clan_members TO admin_user;


--
-- Name: TABLE clans; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.clans TO admin_user;


--
-- Name: SEQUENCE clans_clan_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.clans_clan_id_seq TO admin_user;


--
-- Name: TABLE friends; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.friends TO admin_user;


--
-- Name: TABLE inapps; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.inapps TO admin_user;


--
-- Name: TABLE players; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON TABLE public.players TO admin_user;


--
-- Name: SEQUENCE players_player_id_seq; Type: ACL; Schema: public; Owner: postgres
--

GRANT ALL ON SEQUENCE public.players_player_id_seq TO admin_user;


--
-- PostgreSQL database dump complete
--

