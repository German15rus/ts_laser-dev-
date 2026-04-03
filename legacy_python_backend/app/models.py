from sqlalchemy import Column, Integer, String, Text, Boolean, Date, DateTime, Enum as SQLEnum
from sqlalchemy.sql import func
from app.database import Base
import enum


class ClientStatus(str, enum.Enum):
    ACTIVE = "active"           # Р’ РїСЂРѕС†РµСЃСЃРµ (СЂР°РЅРµРµ "РђРєС‚РёРІРЅС‹Р№")
    COMPLETED = "completed"     # РЈРґР°Р»РµРЅРёРµ Р·Р°РІРµСЂС€РµРЅРѕ
    STOPPED = "stopped"         # РџРµСЂРµСЃС‚Р°Р» С…РѕРґРёС‚СЊ
    LOST = "lost"               # РџРѕС‚РµСЂСЏР»СЃСЏ


class Gender(str, enum.Enum):
    MALE = "Рњ"
    FEMALE = "Р–"


class Partner(Base):
    __tablename__ = "partners"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(255), nullable=False, index=True)
    contacts = Column(Text, nullable=True)  # С‚РµР»РµС„РѕРЅ, instagram, telegram
    type = Column(String(100), nullable=True)  # С‚Р°С‚Сѓ-СЃР°Р»РѕРЅ, РјР°СЃС‚РµСЂ Рё С‚.Рї.
    terms = Column(Text, nullable=True)  # СѓСЃР»РѕРІРёСЏ РїР°СЂС‚РЅРµСЂСЃС‚РІР°
    comment = Column(Text, nullable=True)
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), server_default=func.now(), onupdate=func.now())


class Client(Base):
    __tablename__ = "clients"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(255), nullable=False, index=True)  # Р¤РРћ
    phone = Column(String(20), nullable=True)  # РќРѕРјРµСЂ С‚РµР»РµС„РѕРЅР°
    birth_date = Column(Date, nullable=True)  # Р”Р°С‚Р° СЂРѕР¶РґРµРЅРёСЏ
    age = Column(Integer, nullable=True)  # Р’РѕР·СЂР°СЃС‚ (РїРѕР»РЅС‹С… Р»РµС‚)
    gender = Column(String(1), nullable=True)  # РџРѕР» (Рњ/Р–)
    address = Column(Text, nullable=True)  # РђРґСЂРµСЃ
    referral_partner_id = Column(Integer, nullable=True)  # ID РїР°СЂС‚РЅРµСЂР° (РµСЃР»Рё РёР· РїР°СЂС‚РЅРµСЂСЃРєРѕР№ Р±Р°Р·С‹)
    referral_custom = Column(String(255), nullable=True)  # РЎРІРѕР№ РІР°СЂРёР°РЅС‚ "РєР°Рє СѓР·РЅР°Р»Рё"
    status = Column(String(20), default=ClientStatus.ACTIVE.value)  # РЎС‚Р°С‚СѓСЃ РєР»РёРµРЅС‚Р°
    stopped_reason = Column(Text, nullable=True)  # РџСЂРёС‡РёРЅР° СѓС…РѕРґР° (РµСЃР»Рё status=stopped)
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), server_default=func.now(), onupdate=func.now())


class Tattoo(Base):
    __tablename__ = "tattoos"

    id = Column(Integer, primary_key=True, index=True)
    client_id = Column(Integer, nullable=False, index=True)  # FK РЅР° Client
    name = Column(String(255), nullable=False)  # РќР°Р·РІР°РЅРёРµ С‚Р°С‚СѓРёСЂРѕРІРєРё
    removal_zone = Column(String(255), nullable=True)  # Р—РѕРЅР° СѓРґР°Р»РµРЅРёСЏ (РЅР°РїСЂРёРјРµСЂ: Р»РµРІР°СЏ СЂСѓРєР°, СЃРїРёРЅР°)
    corrections_count = Column(String(100), nullable=True)  # РљРѕР»РёС‡РµСЃС‚РІРѕ РєРѕСЂСЂРµРєС†РёР№/РїРµСЂРµРєСЂС‹С‚РёР№
    last_pigment_date = Column(Date, nullable=True)  # Р”Р°С‚Р° РїРѕСЃР»РµРґРЅРµРіРѕ РІРЅРµСЃРµРЅРёСЏ РїРёРіРјРµРЅС‚Р°
    last_laser_date = Column(Date, nullable=True)  # Р”Р°С‚Р° РїРѕСЃР»РµРґРЅРµРіРѕ СѓРґР°Р»РµРЅРёСЏ Р»Р°Р·РµСЂРѕРј
    no_laser_before = Column(Boolean, default=False)  # РќРµ СѓРґР°Р»СЏР» Р»Р°Р·РµСЂРѕРј СЂР°РЅРµРµ
    previous_removal_place = Column(String(255), nullable=True)  # РњРµСЃС‚Рѕ, РіРґРµ СѓРґР°Р»СЏР»Рё
    desired_result = Column(Text, nullable=True)  # Р–РµР»Р°РµРјС‹Р№ СЂРµР·СѓР»СЊС‚Р°С‚
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), server_default=func.now(), onupdate=func.now())


class LaserSession(Base):
    __tablename__ = "laser_sessions"

    id = Column(Integer, primary_key=True, index=True)
    client_id = Column(Integer, nullable=False, index=True)  # FK РЅР° Client
    tattoo_id = Column(Integer, nullable=True, index=True)  # FK РЅР° Tattoo (РѕРїС†РёРѕРЅР°Р»СЊРЅРѕ)
    tattoo_name = Column(String(255), nullable=True)  # РќР°Р·РІР°РЅРёРµ С‚Р°С‚Сѓ (РґР»СЏ РѕР±СЂР°С‚РЅРѕР№ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё)
    session_number = Column(Integer, nullable=True)  # в„– СЃРµР°РЅСЃР°
    sub_session = Column(String(10), nullable=True)  # РџРѕРґСЃРµР°РЅСЃ (1, 2, 3 РёР»Рё РїСѓСЃС‚Рѕ)
    wavelength = Column(String(100), nullable=True)  # Р”Р»РёРЅР° РІРѕР»РЅС‹
    diameter = Column(String(100), nullable=True)  # Р”РёР°РјРµС‚СЂ
    density = Column(String(100), nullable=True)  # РџР»РѕС‚РЅРѕСЃС‚СЊ
    hertz = Column(String(100), nullable=True)  # Р“РµСЂС†
    flashes_count = Column(Integer, nullable=True)  # РљРѕР»РёС‡РµСЃС‚РІРѕ РІСЃРїС‹С€РµРє
    session_date = Column(Date, nullable=True)  # Р”Р°С‚Р° СЃРµР°РЅСЃР°
    break_period = Column(String(100), nullable=True)  # РџРµСЂРµСЂС‹РІ
    comment = Column(Text, nullable=True)  # РљРѕРјРјРµРЅС‚Р°СЂРёР№
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), server_default=func.now(), onupdate=func.now())


class IntakeSubmission(Base):
    __tablename__ = "intake_submissions"

    id = Column(Integer, primary_key=True, index=True)
    client_id = Column(Integer, nullable=False, index=True)
    tattoo_id = Column(Integer, nullable=True, index=True)

    full_name = Column(String(255), nullable=False)
    phone = Column(String(20), nullable=False, index=True)
    birth_date = Column(Date, nullable=True)
    address = Column(Text, nullable=True)

    referral_source = Column(String(255), nullable=True)
    tattoo_type = Column(String(255), nullable=True)
    tattoo_age = Column(String(255), nullable=True)
    corrections_info = Column(Text, nullable=True)
    previous_removal_info = Column(Text, nullable=True)
    previous_removal_where = Column(Text, nullable=True)
    desired_result = Column(Text, nullable=True)

    source = Column(String(50), nullable=False, default="landing")
    is_new_client = Column(Boolean, nullable=False, default=False)
    raw_payload = Column(Text, nullable=True)

    created_at = Column(DateTime(timezone=True), server_default=func.now())
