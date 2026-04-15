-- ============================================================
-- ИСПРАВЛЕННЫЙ СКРИПТ - БЕЗ CREATE DATABASE
-- ПРЕДПОЛАГАЕТСЯ, ЧТО БАЗА ДАННЫХ УЖЕ СОЗДАНА
-- ============================================================

-- Сначала создайте БД вручную или отдельной командой:
-- CREATE DATABASE poseshcheniya_3nf ENCODING 'UTF8';

-- Затем выполните этот код, уже подключившись к БД poseshcheniya_3nf

-- 1. ТАБЛИЦЫ-СПРАВОЧНИКИ (СЛОВАРИ)
CREATE TABLE IF NOT EXISTS подразделения (
    код_подразделения SERIAL PRIMARY KEY,
    название VARCHAR(100) NOT NULL UNIQUE,
    описание TEXT NULL
);

CREATE TABLE IF NOT EXISTS отделы (
    код_отдела SERIAL PRIMARY KEY,
    название VARCHAR(100) NOT NULL UNIQUE,
    описание TEXT NULL
);

CREATE TABLE IF NOT EXISTS должности (
    код_должности SERIAL PRIMARY KEY,
    название VARCHAR(100) NOT NULL UNIQUE,
    оклад NUMERIC(10,2) NULL,
    описание TEXT NULL
);

CREATE TABLE IF NOT EXISTS типы_посещений (
    код_типа SERIAL PRIMARY KEY,
    название VARCHAR(50) NOT NULL UNIQUE,
    описание TEXT NULL
);

CREATE TABLE IF NOT EXISTS статусы_посещений (
    код_статуса SERIAL PRIMARY KEY,
    название VARCHAR(50) NOT NULL UNIQUE,
    описание TEXT NULL
);

CREATE TABLE IF NOT EXISTS группы (
    код_группы SERIAL PRIMARY KEY,
    название VARCHAR(50) NOT NULL UNIQUE,
    описание TEXT NULL,
    максимальное_количество INTEGER DEFAULT 10
);

-- 2. ОСНОВНЫЕ ТАБЛИЦЫ
CREATE TABLE IF NOT EXISTS сотрудники (
    код_сотрудника INTEGER PRIMARY KEY,
    фамилия VARCHAR(50) NOT NULL,
    имя VARCHAR(50) NOT NULL,
    отчество VARCHAR(50) NULL,
    код_подразделения INTEGER NULL REFERENCES подразделения(код_подразделения),
    код_отдела INTEGER NULL REFERENCES отделы(код_отдела),
    код_должности INTEGER NULL REFERENCES должности(код_должности),
    телефон_служебный VARCHAR(50) NULL,
    email_служебный VARCHAR(100) NULL UNIQUE,
    дата_приема DATE NULL,
    дата_увольнения DATE NULL,
    активно BOOLEAN DEFAULT TRUE,
    
    CONSTRAINT проверка_подразделение_или_отдел CHECK (
        (код_подразделения IS NOT NULL OR код_отдела IS NOT NULL)
    )
);

CREATE TABLE IF NOT EXISTS посетители (
    код_посетителя SERIAL PRIMARY KEY,
    фамилия VARCHAR(50) NOT NULL,
    имя VARCHAR(50) NOT NULL,
    отчество VARCHAR(50) NULL,
    номер_телефона VARCHAR(50) NULL,
    email VARCHAR(100) NULL UNIQUE,
    дата_рождения DATE NULL,
    серия_паспорта VARCHAR(4) NULL,
    номер_паспорта VARCHAR(6) NULL,
    логин VARCHAR(50) NOT NULL UNIQUE,
    пароль VARCHAR(100) NOT NULL,
    дата_регистрации DATE DEFAULT CURRENT_DATE,
    активно BOOLEAN DEFAULT TRUE,
    
    CONSTRAINT уникальный_паспорт UNIQUE (серия_паспорта, номер_паспорта),
    CONSTRAINT проверка_паспорта CHECK (
        (серия_паспорта IS NULL AND номер_паспорта IS NULL) OR
        (серия_паспорта ~ '^[0-9]{4}$' AND номер_паспорта ~ '^[0-9]{6}$')
    )
);

CREATE TABLE IF NOT EXISTS посещения (
    код_посещения SERIAL PRIMARY KEY,
    код_посетителя INTEGER NOT NULL REFERENCES посетители(код_посетителя),
    код_сотрудника INTEGER NOT NULL REFERENCES сотрудники(код_сотрудника),
    код_типа INTEGER NOT NULL REFERENCES типы_посещений(код_типа),
    код_статуса INTEGER NOT NULL REFERENCES статусы_посещений(код_статуса) DEFAULT 1,
    код_группы INTEGER NULL REFERENCES группы(код_группы),
    дата_посещения DATE NOT NULL DEFAULT CURRENT_DATE,
    время_входа TIME NULL,
    время_выхода TIME NULL,
    цель_визита TEXT NULL,
    примечание TEXT NULL,
    
    CONSTRAINT уникальное_посещение_в_день UNIQUE (код_посетителя, дата_посещения)
);

-- 3. ИНДЕКСЫ
CREATE INDEX IF NOT EXISTS idx_сотрудники_фио ON сотрудники(фамилия, имя, отчество);
CREATE INDEX IF NOT EXISTS idx_посетители_фио ON посетители(фамилия, имя, отчество);
CREATE INDEX IF NOT EXISTS idx_посетители_логин ON посетители(логин);
CREATE INDEX IF NOT EXISTS idx_посещения_дата ON посещения(дата_посещения);
CREATE INDEX IF NOT EXISTS idx_посещения_сотрудник ON посещения(код_сотрудника);
CREATE INDEX IF NOT EXISTS idx_посещения_посетитель ON посещения(код_посетителя);

-- 4. ЗАПОЛНЕНИЕ СПРАВОЧНИКОВ
INSERT INTO типы_посещений (название, описание) VALUES
('личное', 'Индивидуальное посещение сотрудника'),
('групповое', 'Посещение в составе группы')
ON CONFLICT (название) DO NOTHING;

INSERT INTO статусы_посещений (название, описание) VALUES
('запланировано', 'Посещение запланировано'),
('активно', 'Посетитель на объекте'),
('завершено', 'Посещение завершено'),
('отменено', 'Посещение отменено')
ON CONFLICT (название) DO NOTHING;

INSERT INTO подразделения (название) VALUES
('Производство'),
('Сбыт'),
('Администрация'),
('Служба безопасности'),
('Планирование')
ON CONFLICT (название) DO NOTHING;

INSERT INTO отделы (название) VALUES
('Общий отдел'),
('Охрана'),
('Бухгалтерия'),
('IT-отдел')
ON CONFLICT (название) DO NOTHING;

INSERT INTO должности (название, оклад) VALUES
('Начальник производства', 80000),
('Руководитель отдела сбыта', 75000),
('Администратор', 50000),
('Начальник безопасности', 70000),
('Плановик', 55000),
('Специалист', 45000),
('Охранник', 35000)
ON CONFLICT (название) DO NOTHING;

INSERT INTO группы (название, максимальное_количество) VALUES
('ГР1', 10),
('ГР2', 10)
ON CONFLICT (название) DO NOTHING;

-- 5. ЗАПОЛНЕНИЕ СОТРУДНИКОВ
INSERT INTO сотрудники (код_сотрудника, фамилия, имя, отчество, код_подразделения, код_отдела, код_должности) VALUES
(9367788, 'Фомичева', 'Авдотья', 'Трофимовна', (SELECT код_подразделения FROM подразделения WHERE название='Производство'), NULL, (SELECT код_должности FROM должности WHERE название='Начальник производства')),
(9788737, 'Гаврилова', 'Римма', 'Ефимовна', (SELECT код_подразделения FROM подразделения WHERE название='Сбыт'), NULL, (SELECT код_должности FROM должности WHERE название='Руководитель отдела сбыта')),
(9736379, 'Носкова', 'Наталия', 'Прохоровна', (SELECT код_подразделения FROM подразделения WHERE название='Администрация'), NULL, (SELECT код_должности FROM должности WHERE название='Администратор')),
(9362832, 'Архипов', 'Тимофей', 'Васильевич', (SELECT код_подразделения FROM подразделения WHERE название='Служба безопасности'), NULL, (SELECT код_должности FROM должности WHERE название='Начальник безопасности')),
(9737848, 'Орехова', 'Вероника', 'Артемовна', (SELECT код_подразделения FROM подразделения WHERE название='Планирование'), NULL, (SELECT код_должности FROM должности WHERE название='Плановик')),
(9768239, 'Савельев', 'Павел', 'Степанович', NULL, (SELECT код_отдела FROM отделы WHERE название='Общий отдел'), (SELECT код_должности FROM должности WHERE название='Специалист')),
(9404040, 'Чернов', 'Всеволод', 'Наумович', NULL, (SELECT код_отдела FROM отделы WHERE название='Охрана'), (SELECT код_должности FROM должности WHERE название='Охранник'))
ON CONFLICT (код_сотрудника) DO NOTHING;

-- 6. ЗАПОЛНЕНИЕ ПОСЕТИТЕЛЕЙ (пример)
INSERT INTO посетители (фамилия, имя, отчество, номер_телефона, email, логин, пароль) VALUES
('Степанова', 'Радинка', 'Власовна', '+7 (613) 272-60-62', 'Radinka100@yandex.ru', 'Vlas86', 'b3uWS6#Thuvq'),
('Шилов', 'Прохор', 'Герасимович', '+7 (615) 594-77-66', 'Prohor156@list.ru', 'Prohor156', 'zDdom}SIhWs?')
ON CONFLICT (логин) DO NOTHING;

-- Проверка результата
SELECT 'Таблицы успешно созданы!' AS Статус;
SELECT COUNT(*) AS Количество_таблиц FROM information_schema.tables WHERE table_schema = 'public';