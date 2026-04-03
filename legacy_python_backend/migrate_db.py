#!/usr/bin/env python3
"""
Скрипт миграции базы данных TS Laser CRM
Конвертирует поля wavelength, diameter, density, hertz из INTEGER в TEXT

Запуск: python migrate_db.py
"""

import sqlite3
import sys
import shutil
import os

DB_PATH = "tslaser.db"


def migrate():
    # Проверяем существование базы данных
    if not os.path.exists(DB_PATH):
        print(f"❌ База данных {DB_PATH} не найдена!")
        print("   Убедитесь, что запускаете скрипт из папки с проектом.")
        sys.exit(1)
    
    print("🔄 Проверяю базу данных...")
    
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()
    
    # Проверяем существование таблицы laser_sessions
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='laser_sessions'")
    if not cursor.fetchone():
        print("✅ Таблица laser_sessions не найдена (новая база). Миграция не требуется.")
        conn.close()
        return
    
    # Проверяем текущий тип колонки wavelength
    cursor.execute("PRAGMA table_info(laser_sessions)")
    columns = {row[1]: row[2] for row in cursor.fetchall()}
    
    # Если wavelength уже TEXT/VARCHAR - миграция не нужна
    if 'wavelength' in columns:
        col_type = columns['wavelength'].upper()
        if 'INT' not in col_type:
            print("✅ База данных уже обновлена, миграция не требуется.")
            conn.close()
            return
    
    print("📦 Создаю резервную копию...")
    backup_path = DB_PATH + ".backup"
    shutil.copy(DB_PATH, backup_path)
    print(f"   Копия сохранена: {backup_path}")
    
    print("🔧 Выполняю миграцию...")
    
    try:
        # Создаём новую таблицу с текстовыми полями
        cursor.execute("""
        CREATE TABLE IF NOT EXISTS laser_sessions_new (
            id INTEGER PRIMARY KEY,
            client_id INTEGER NOT NULL,
            tattoo_id INTEGER,
            tattoo_name VARCHAR(255),
            session_number INTEGER,
            sub_session VARCHAR(10),
            wavelength VARCHAR(100),
            diameter VARCHAR(100),
            density VARCHAR(100),
            hertz VARCHAR(100),
            flashes_count INTEGER,
            session_date DATE,
            break_period VARCHAR(100),
            comment TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
        """)
        
        # Копируем данные с конвертацией
        cursor.execute("""
        INSERT INTO laser_sessions_new 
            (id, client_id, tattoo_id, tattoo_name, session_number, sub_session,
             wavelength, diameter, density, hertz, flashes_count, session_date,
             break_period, comment, created_at, updated_at)
        SELECT 
            id, client_id, tattoo_id, tattoo_name, session_number, sub_session,
            CAST(wavelength AS TEXT), CAST(diameter AS TEXT), 
            CAST(density AS TEXT), CAST(hertz AS TEXT),
            flashes_count, session_date, break_period, comment, created_at, updated_at
        FROM laser_sessions
        """)
        
        # Считаем сколько записей перенесли
        cursor.execute("SELECT COUNT(*) FROM laser_sessions_new")
        count = cursor.fetchone()[0]
        
        # Удаляем старую таблицу и переименовываем новую
        cursor.execute("DROP TABLE laser_sessions")
        cursor.execute("ALTER TABLE laser_sessions_new RENAME TO laser_sessions")
        
        # Создаём индексы
        cursor.execute("CREATE INDEX IF NOT EXISTS ix_laser_sessions_id ON laser_sessions (id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS ix_laser_sessions_client_id ON laser_sessions (client_id)")
        cursor.execute("CREATE INDEX IF NOT EXISTS ix_laser_sessions_tattoo_id ON laser_sessions (tattoo_id)")
        
        conn.commit()
        conn.close()
        
        print(f"✅ Миграция успешно завершена!")
        print(f"   Перенесено записей: {count}")
        print("")
        print("   Теперь можно запускать приложение:")
        print("   python run.py")
        
    except Exception as e:
        conn.rollback()
        conn.close()
        print(f"❌ Ошибка при миграции: {e}")
        print(f"   База данных восстановлена из резервной копии: {backup_path}")
        raise


if __name__ == "__main__":
    try:
        migrate()
    except Exception as e:
        print(f"\n❌ Критическая ошибка: {e}")
        sys.exit(1)
