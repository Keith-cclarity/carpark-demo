# Technical Requirements Document — Smart Parking Navigator

| Field        | Value                        |
|--------------|------------------------------|
| Version      | 1.0.0                        |
| Status       | Draft                        |
| Author       | Copilot Coding Agent         |
| Created      | 2026-08-19                   |
| Last Updated | 2026-08-19                   |
| Issue        | #2                           |
| PRD          | PRD.md v1.0.0                |

---

## 1. Solution Structure

The solution uses .NET Aspire and consists of four projects:

| Project                              | Role                                                                  |
|--------------------------------------|-----------------------------------------------------------------------|
| `CarparkAvailability.AppHost`        | Aspire orchestrator; injects secrets and wires service references.   |
| `CarparkAvailability.ServiceDefaults`| Shared telemetry, health checks, and resilience defaults.            |
| `CarparkAvailability.ApiApp`         | Backend ASP.NET Core Minimal API; owns all server-side data access.  |
| `CarparkAvailability.WebApp`         | Blazor Server (or Blazor WebAssembly) frontend; owns UI and map.     |

No new projects are to be added for the MVP.

---

## 2. Secret Boundaries

| Secret                | Owner project   | Injection mechanism                                              | Must NOT appear in              |
|-----------------------|-----------------|------------------------------------------------------------------|---------------------------------|
| `DataGovSg:ApiKey`    | AppHost secrets | `WithEnvironment("DataGovSg__ApiKey", …)` → ApiApp env var      | WebApp, browser, logs           |
| `GoogleMaps:ApiKey`   | AppHost secrets | `WithEnvironment("GoogleMaps__ApiKey", …)` → WebApp env var     | ApiApp, server logs             |

The AppHost already wires both secrets (see `AppHost.cs`). No changes to this
wiring are required.

---

## 3. HDB CSV Ingestion (`ApiApp`)

### 3.1 Data source

`data/HDBCarparkInformation.csv` is embedded as a build artifact and loaded
once at startup.

### 3.2 Schema

| Column                  | Type    | Notes                                                        |
|-------------------------|---------|--------------------------------------------------------------|
| `car_park_no`           | string  | Primary join key to `carpark_number` in the live API.        |
| `address`               | string  |                                                              |
| `x_coord`               | decimal | SVY21 easting; must be converted to WGS84 longitude.        |
| `y_coord`               | decimal | SVY21 northing; must be converted to WGS84 latitude.        |
| `car_park_type`         | string  | e.g. SURFACE CAR PARK, MULTI-STOREY CAR PARK, BASEMENT CAR PARK |
| `type_of_parking_system`| string  | ELECTRONIC PARKING or COUPON PARKING                        |
| `short_term_parking`    | string  | Raw value; no interpretation of undocumented codes.         |
| `free_parking`          | string  | Raw value; no interpretation.                               |
| `night_parking`         | string  | YES or NO                                                   |
| `car_park_decks`        | integer | String in CSV; parse safely; treat non-numeric as 0.        |
| `gantry_height`         | decimal | String in CSV; parse safely; treat 0 as "N/A" in UI.       |
| `car_park_basement`     | string  | Y or N                                                      |

### 3.3 Validation

- Rows with a missing or empty `car_park_no` must be skipped and logged as
  warnings.
- Numeric columns (`x_coord`, `y_coord`, `car_park_decks`, `gantry_height`)
  that cannot be parsed are set to their zero/default value and logged.
- The total row count and skip count are logged at startup.

### 3.4 SVY21-to-WGS84 conversion

SVY21 uses the Cassini-Soldner projection centred at (1.366666, 103.833333).
Implement the standard SVY21 conversion algorithm (as published by the
Singapore Land Authority) to produce WGS84 latitude and longitude.

The conversion must be unit-tested with known reference points.

---

## 4. data.gov.sg Carpark Availability API (`ApiApp`)

### 4.1 Endpoint

```
GET https://api.data.gov.sg/v1/transport/carpark-availability
```

The `date_time` query parameter may be omitted to retrieve the latest snapshot.
The `ApiKey` is sent in the `Authorization: ****** header.

### 4.2 Response schema

The live response is documented in `data/CarparkAvailability.json` (OpenAPI
specification) and a representative sample is in
`data/carpark-availability-sample.json`.

Key fields used by the application:

```
items[].timestamp                       – ISO 8601 snapshot time (UTC+8)
items[].carpark_data[].carpark_number   – join key
items[].carpark_data[].update_datetime  – ISO 8601; no timezone suffix
items[].carpark_data[].carpark_info[].lot_type
items[].carpark_data[].carpark_info[].total_lots     – string; parse to int
items[].carpark_data[].carpark_info[].lots_available – string; parse to int
```

### 4.3 Polling

- ApiApp polls the endpoint **at most once per minute** using a background
  `IHostedService`.
- The most recent successful response is held in memory (last-known-good cache).
- A `DateTimeOffset` records when the cache was last successfully refreshed.

### 4.4 Schema validation

- The published OpenAPI specification (`data/CarparkAvailability.json`) is the
  primary contract.
- If required fields are absent or types are incompatible the poll cycle logs an
  error and retains the previous cache entry.
- Unknown optional fields are accepted without error.

### 4.5 Error handling

| Condition                            | Behaviour                                               |
|--------------------------------------|---------------------------------------------------------|
| HTTP 4xx / 5xx from upstream         | Log, retain cache, increment error counter.            |
| Network timeout                      | Log, retain cache, increment error counter.            |
| JSON parse failure                   | Log schema error details, retain cache.                |
| Cache empty on first poll failure    | Return 503 from the API endpoint with a clear message. |

---

## 5. Joining Static and Live Data (`ApiApp`)

- Join key: `car_park_no` (CSV) = `carpark_number` (API); case-insensitive
  exact match after trimming whitespace.
- Car parks with live data but no matching CSV row are included in the response
  with only the live fields populated and a `staticDataAvailable: false` flag.
- Car parks with CSV data but no live match are excluded from the API response
  (they have no availability to display).

---

## 6. Distance Calculation (`ApiApp`)

- Use the Haversine formula to calculate great-circle distance between each
  car park's WGS84 position and the queried destination coordinates.
- Return distance in metres, rounded to the nearest integer.
- The 500 m radius filter is applied server-side.

---

## 7. API Contracts (`ApiApp` ↔ `WebApp`)

### 7.1 `GET /api/carparks/nearby`

Query parameters:

| Name   | Type   | Required | Description                           |
|--------|--------|----------|---------------------------------------|
| `lat`  | double | Yes      | WGS84 latitude of destination         |
| `lng`  | double | Yes      | WGS84 longitude of destination        |
| `radius` | int  | No       | Search radius in metres (default 500) |

Response body (200 OK):

```jsonc
{
  "snapshotTime": "2026-08-10T21:29:37+08:00",  // latest API timestamp
  "cacheAge": 42,                                 // seconds since last poll
  "carParks": [
    {
      "carParkNo": "HE12",
      "address": "BLK 51 CIRCUIT ROAD",
      "latitude": 1.3244,
      "longitude": 103.8765,
      "distanceMetres": 312,
      "carParkType": "MULTI-STOREY CAR PARK",
      "parkingSystem": "ELECTRONIC PARKING",
      "shortTermParking": "WHOLE DAY",
      "freeParking": "SUN & PH FR 7AM-10.30PM",
      "nightParking": true,
      "carParkDecks": 5,
      "gantryHeight": 2.1,
      "carParkBasement": false,
      "staticDataAvailable": true,
      "updateDatetime": "2026-08-10T21:28:38",
      "isStale": false,
      "lots": [
        { "lotType": "C", "totalLots": 105, "lotsAvailable": 30 }
      ]
    }
  ]
}
```

`isStale` is `true` when `updateDatetime` is more than 5 minutes before
`snapshotTime`.

Error responses use RFC 7807 Problem Details.

### 7.2 `GET /api/carparks/{carParkNo}`

Returns the same shape as a single element of `carParks` above, or 404 if the
car park number is not known.

---

## 8. WebApp (`CarparkAvailability.WebApp`)

### 8.1 Google Maps integration

- Load the Maps JavaScript API, Places API (New), and Geocoding API via a
  `<script>` tag using the `GoogleMaps__ApiKey` environment variable.
- The API key is rendered server-side into the page; it must not be stored in
  `appsettings.json` or any committed file.
- Restrict the key to website origins in the Google Cloud console (see
  `docs/google-maps-api-key.md`).

### 8.2 State machine per car-park list

| State       | Trigger                                                    |
|-------------|------------------------------------------------------------|
| Loading     | Request to `/api/carparks/nearby` is in flight.           |
| Loaded      | Response received with ≥ 1 car park.                      |
| Empty       | Response received with 0 car parks within the radius.     |
| Stale       | `isStale = true` on at least one car park.                |
| Unavailable | 503 response or network error; no previous data held.     |
| Error       | Any unexpected non-503 error response.                    |
| LastKnownGood | 503 response but previous data is available in memory.  |

### 8.3 Destination search

- Use the Places Autocomplete widget restricted to country `SG`.
- On selection, obtain the `geometry.location` from the Places result and pass
  `lat`/`lng` to the carparks API.

### 8.4 Accessibility

- Colour is not the sole means of conveying freshness or state; text labels or
  icons with `aria-label` must accompany colour indicators.
- Minimum contrast ratio 4.5:1 for normal text (WCAG 2.1 AA).

---

## 9. Security

| Concern                         | Mitigation                                                        |
|---------------------------------|-------------------------------------------------------------------|
| data.gov.sg key exposure        | Held only in ApiApp environment; never forwarded to WebApp.      |
| Google Maps key exposure        | Restricted by HTTP referrer in Google Cloud console.             |
| Log injection                   | Log structured data; do not interpolate raw API responses.       |
| Input validation                | `lat`, `lng`, and `radius` parameters are validated server-side. |
| HTTPS                           | Aspire enforces HTTPS for external endpoints.                    |

---

## 10. Testing

| Layer               | Approach                                                                              |
|---------------------|---------------------------------------------------------------------------------------|
| SVY21 conversion    | Unit tests with SLA reference coordinates; tolerance ≤ 0.5 m.                       |
| CSV parser          | Unit tests for normal rows, missing fields, and non-numeric values.                  |
| Join logic          | Unit tests for matched, unmatched-CSV, and unmatched-live scenarios.                 |
| Distance filter     | Unit tests for radius boundary (exactly 500 m, 499 m, 501 m).                       |
| Stale detection     | Unit tests for the 5-minute threshold (exactly 5 min, 4 min 59 s, 5 min 01 s).      |
| API contract        | Integration/contract tests that validate a representative live response against the  |
|                     | OpenAPI spec; run in CI with a recorded fixture.                                     |
| API endpoints       | Integration tests using `WebApplicationFactory` for `/api/carparks/nearby` and      |
|                     | `/api/carparks/{carParkNo}`, covering happy path, empty result, and 503 scenarios.  |
| UI states           | Playwright or bUnit component tests covering Loading, Loaded, Empty, Stale,         |
|                     | Unavailable, Error, and LastKnownGood states.                                        |

---

## 11. Out of Scope (Technical)

The following are explicitly out of scope for the MVP implementation:

- Persistent databases or caching layers (Redis, SQL, etc.)
- User authentication and authorisation
- Deployment pipelines and cloud infrastructure
- MCP server or client integration
- Agentic AI or LLM integration (deferred to step 05)
- Push notifications and WebSockets
- Occupancy forecasting or historical data storage
