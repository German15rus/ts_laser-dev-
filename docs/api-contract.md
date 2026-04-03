# API Contract (Compatibility)

## Auth

- `POST /api/login`
- `POST /api/logout`
- `GET /api/auth/check`

## Partners

- `GET /api/partners`
- `POST /api/partners`
- `GET /api/partners/{partner_id}`
- `PUT /api/partners/{partner_id}`
- `DELETE /api/partners/{partner_id}`

## Clients

- `GET /api/clients`
- `POST /api/clients`
- `GET /api/clients/{client_id}`
- `PUT /api/clients/{client_id}`
- `DELETE /api/clients/{client_id}`

## Tattoos

- `GET /api/clients/{client_id}/tattoos`
- `POST /api/clients/{client_id}/tattoos`
- `GET /api/tattoos/{tattoo_id}`
- `PUT /api/tattoos/{tattoo_id}`
- `DELETE /api/tattoos/{tattoo_id}`

## Sessions

- `GET /api/clients/{client_id}/sessions`
- `POST /api/clients/{client_id}/sessions`
- `GET /api/sessions/{session_id}`
- `PUT /api/sessions/{session_id}`
- `DELETE /api/sessions/{session_id}`

## Export

- `GET /api/export/clients?format=csv|xlsx`
- `GET /api/export/partners?format=csv|xlsx`
- `GET /api/clients/{client_id}/export/sessions?format=csv|xlsx`
- `GET /api/clients/{client_id}/export/tattoos?format=csv|xlsx`

## Public Booking

- `POST /api/public/booking`

## Pages

- `GET /`
- `GET /login`
- `GET /clients/{client_id}/sessions`
- `GET /booking`
- `GET /book`
