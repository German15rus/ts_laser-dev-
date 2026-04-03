from pydantic import BaseModel, Field
from typing import Optional
from datetime import date, datetime
from enum import Enum


class ClientStatus(str, Enum):
    ACTIVE = "active"
    COMPLETED = "completed"
    STOPPED = "stopped"
    LOST = "lost"


# ============== Partner Schemas ==============

class PartnerBase(BaseModel):
    name: str = Field(..., min_length=1, max_length=255)
    contacts: Optional[str] = None
    type: Optional[str] = None
    terms: Optional[str] = None
    comment: Optional[str] = None


class PartnerCreate(PartnerBase):
    pass


class PartnerUpdate(BaseModel):
    name: Optional[str] = Field(None, min_length=1, max_length=255)
    contacts: Optional[str] = None
    type: Optional[str] = None
    terms: Optional[str] = None
    comment: Optional[str] = None


class PartnerResponse(PartnerBase):
    id: int
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True


# ============== Client Schemas ==============

class ClientBase(BaseModel):
    name: str = Field(..., min_length=1, max_length=255)
    phone: Optional[str] = None
    birth_date: Optional[date] = None
    age: Optional[int] = None
    gender: Optional[str] = None
    address: Optional[str] = None
    referral_partner_id: Optional[int] = None
    referral_custom: Optional[str] = None
    status: ClientStatus = ClientStatus.ACTIVE
    stopped_reason: Optional[str] = None


class ClientCreate(ClientBase):
    pass


class ClientUpdate(BaseModel):
    name: Optional[str] = Field(None, min_length=1, max_length=255)
    phone: Optional[str] = None
    birth_date: Optional[date] = None
    age: Optional[int] = None
    gender: Optional[str] = None
    address: Optional[str] = None
    referral_partner_id: Optional[int] = None
    referral_custom: Optional[str] = None
    status: Optional[ClientStatus] = None
    stopped_reason: Optional[str] = None


class ClientResponse(ClientBase):
    id: int
    created_at: datetime
    updated_at: datetime
    referral_partner_name: Optional[str] = None  # РРјСЏ РїР°СЂС‚РЅРµСЂР° РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ

    class Config:
        from_attributes = True


class ClientListResponse(BaseModel):
    id: int
    name: str
    phone: Optional[str] = None
    address: Optional[str] = None
    status: str
    created_at: datetime

    class Config:
        from_attributes = True


# ============== Tattoo Schemas ==============

class TattooBase(BaseModel):
    name: str = Field(..., min_length=1, max_length=255)
    removal_zone: Optional[str] = None
    corrections_count: Optional[str] = None
    last_pigment_date: Optional[date] = None
    last_laser_date: Optional[date] = None
    no_laser_before: bool = False
    previous_removal_place: Optional[str] = None
    desired_result: Optional[str] = None


class TattooCreate(TattooBase):
    pass


class TattooUpdate(BaseModel):
    name: Optional[str] = Field(None, min_length=1, max_length=255)
    removal_zone: Optional[str] = None
    corrections_count: Optional[str] = None
    last_pigment_date: Optional[date] = None
    last_laser_date: Optional[date] = None
    no_laser_before: Optional[bool] = None
    previous_removal_place: Optional[str] = None
    desired_result: Optional[str] = None


class TattooResponse(TattooBase):
    id: int
    client_id: int
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True


class TattoosListResponse(BaseModel):
    tattoos: list[TattooResponse]
    client_name: str
    client_id: int


# ============== LaserSession Schemas ==============

class LaserSessionBase(BaseModel):
    tattoo_id: Optional[int] = None
    tattoo_name: Optional[str] = None
    session_number: Optional[int] = None
    sub_session: Optional[str] = None
    wavelength: Optional[str] = None
    diameter: Optional[str] = None
    density: Optional[str] = None
    hertz: Optional[str] = None
    flashes_count: Optional[int] = None
    session_date: Optional[date] = None
    break_period: Optional[str] = None
    comment: Optional[str] = None


class LaserSessionCreate(LaserSessionBase):
    pass


class LaserSessionUpdate(LaserSessionBase):
    pass


class LaserSessionResponse(LaserSessionBase):
    id: int
    client_id: int
    created_at: datetime
    updated_at: datetime

    class Config:
        from_attributes = True


class LaserSessionsListResponse(BaseModel):
    sessions: list[LaserSessionResponse]
    total_flashes: int
    client_name: str
    client_id: int


# ============== Auth Schemas ==============

class LoginRequest(BaseModel):
    password: str


class LoginResponse(BaseModel):
    success: bool
    message: str


# ============== Public Booking Schemas ==============

class PublicBookingCreate(BaseModel):
    full_name: str = Field(..., min_length=2, max_length=255)
    phone: str = Field(..., min_length=5, max_length=32)
    birth_date: date
    address: str = Field(..., min_length=2, max_length=500)

    referral_source: str = Field(..., min_length=1, max_length=255)
    tattoo_type: str = Field(..., min_length=1, max_length=255)
    tattoo_age: str = Field(..., min_length=1, max_length=255)
    corrections_info: str = Field(..., min_length=1, max_length=500)
    previous_removal_info: str = Field(..., min_length=1, max_length=500)
    previous_removal_where: str = Field(..., min_length=1, max_length=500)
    desired_result: str = Field(..., min_length=1, max_length=500)

    consent_personal_data: bool = Field(...)
    form_started_at: Optional[int] = None
    website: Optional[str] = None


class PublicBookingResponse(BaseModel):
    success: bool
    message: str
    client_id: int
    is_new_client: bool
