# Product Requirements Document — Smart Parking Navigator

| Field       | Value                        |
|-------------|------------------------------|
| Version     | 1.0.0                        |
| Status      | Draft                        |
| Author      | Copilot Coding Agent         |
| Created     | 2026-08-19                   |
| Last Updated| 2026-08-19                   |
| Issue       | #2                           |

---

## 1. Purpose

Smart Parking Navigator helps drivers in Singapore quickly find an available HDB
car park near their destination. It surfaces live lot availability alongside
static car-park details so that drivers can choose a suitable option before they
arrive.

---

## 2. Target Users

| Persona              | Description                                                                               |
|----------------------|-------------------------------------------------------------------------------------------|
| Urban driver         | Drives in Singapore regularly and wants to avoid circling for a parking space.            |
| Occasional driver    | Visits an unfamiliar area and needs to know where to park before leaving home.            |
| Height-restricted vehicle driver | Drives a van or SUV and needs to check gantry-height restrictions in advance. |

---

## 3. Singapore Context

- All car park data comes from HDB (Housing & Development Board) via a static
  CSV dataset and the data.gov.sg Car Park Availability API.
- Coordinates in the HDB dataset use the SVY21 projection and must be converted
  to WGS84 for map display.
- The API is polled at most once per minute (the recommended interval).
- All times are interpreted in Singapore Standard Time (UTC+8).

---

## 4. User Journeys

### 4.1 Find parking near a destination

1. User opens the app; a Google Map centred on Singapore is shown.
2. User types a destination in the search box (restricted to Singapore).
3. The map pans and zooms to the destination; HDB car parks within 500 m are
   listed in a side panel and marked on the map.
4. Each car park card shows: name/address, distance, available lots / total
   lots, occupancy percentage, lot types, and data-freshness indicator.
5. User taps a car park card to see full details.
6. User applies filters (available only, vehicle type, night parking, car-park
   type) to narrow results.

### 4.2 Check car-park details

1. User selects a car park from the list or map marker.
2. A detail panel opens showing all static fields (type, parking system,
   short-term hours, free-parking conditions, night parking, decks, gantry
   height, basement flag) and live availability per lot type.
3. If live data is stale or unavailable the panel shows the last-known-good
   data with a clear warning.

### 4.3 Explore the map

1. User pans or zooms the map without entering a destination.
2. Car parks visible in the current viewport are listed and marked.
3. Results refresh automatically when the viewport changes.

---

## 5. MVP Scope (P0)

The following features are in scope for the MVP.

### 5.1 Destination search

- Free-text search powered by Google Places API (New), restricted to Singapore.
- The map pans and zooms to the chosen place.

### 5.2 Nearby car parks

- Show HDB car parks within **500 m** of the searched destination or map centre.
- Sort by ascending distance by default.

### 5.3 Live lot availability

- Display **available lots** and **total lots** per car park, broken down by
  lot type (C = Car, H = Heavy Vehicle, M = Motorcycle, Y = Seasonal).
- Show the **occupancy rate** (percentage of lots taken).
- Poll the data.gov.sg API at most once per minute.

### 5.4 Data freshness

- Show the `update_datetime` returned by the API for each car park.
- Mark data as **stale** when `update_datetime` is more than 5 minutes behind
  the latest API timestamp.
- Show a distinct **unavailable** state when the API cannot be reached and no
  cached data exists.

### 5.5 Filters

| Filter              | Values                                          |
|---------------------|-------------------------------------------------|
| Available lots only | Toggle: show only car parks with ≥ 1 lot free  |
| Lot type            | Multi-select: C, H, M (Y excluded from MVP)    |
| Night parking       | Toggle: YES only                                |
| Car-park type       | Multi-select: SURFACE, MULTI-STOREY, BASEMENT  |

### 5.6 Car-park detail panel

Display all fields from the HDB CSV:

- Address
- Car-park type
- Parking system (ELECTRONIC / COUPON)
- Short-term parking hours (raw value; no interpretation of undocumented codes)
- Free-parking conditions (raw value; no interpretation)
- Night parking (YES / NO)
- Number of decks
- Gantry height (m); shown as "N/A" when 0
- Basement flag

### 5.7 Loading and error states

| State       | Description                                                      |
|-------------|------------------------------------------------------------------|
| Loading     | Skeleton cards while data is being fetched                       |
| Empty       | Message when no car parks are within 500 m of the destination    |
| Stale       | Warning banner with last-known-good data and timestamp           |
| Unavailable | Error message when live API is unreachable and no cache exists   |
| Error       | Generic error message for unexpected failures                    |

---

## 6. Out of Scope (MVP)

The following are explicitly deferred:

- Favorites and availability alerts
- Occupancy forecasting and historical data
- User accounts and profiles
- Vehicle-profile-based automatic filtering
- Traffic and weather integration
- Databases and persistent storage
- Deployment to production infrastructure
- MCP (Model Context Protocol) tooling
- Agentic AI features (introduced separately in step 05)

---

## 7. Acceptance Criteria

| ID   | Criterion                                                                                                               |
|------|-------------------------------------------------------------------------------------------------------------------------|
| AC-01 | Entering a Singapore address in the search box pans the map to that location within 2 seconds.                        |
| AC-02 | Car parks within 500 m of the destination are shown in ascending distance order.                                      |
| AC-03 | Each car-park card shows available lots, total lots, and occupancy rate for at least the Car (C) lot type.            |
| AC-04 | Available-only filter hides car parks where all lot types have 0 available lots.                                      |
| AC-05 | Night-parking filter shows only car parks with `night_parking = YES`.                                                 |
| AC-06 | Car-park-type filter correctly restricts the list to the selected type(s).                                            |
| AC-07 | The detail panel shows all HDB static fields for the selected car park.                                               |
| AC-08 | When `update_datetime` is > 5 min behind the API timestamp, the stale indicator is shown.                             |
| AC-09 | When the live API fails, the last successfully retrieved data is shown with a warning, not a blank panel.             |
| AC-10 | When no car parks are within 500 m, an empty-state message is displayed.                                              |
| AC-11 | The Google Maps API key is never exposed server-side; the data.gov.sg API key is never exposed to the browser.        |
| AC-12 | The app is usable on a 1280 × 800 desktop viewport without horizontal scrolling.                                      |
