# Test Matrix

Fill this in during Session 05. Every row must map to a real test file — a matrix with
no tests behind it is a wiki page, not a test plan.

Levels: `unit` · `integration` · `contract` · `e2e`

## Backend

| # | Scenario | Input | Expected result | Level | Test file | Status |
|---|---|---|---|---|---|---|
| 1 | Create a valid order | `{"item":"book","quantity":2}` | `201`, body matches `Order` | integration | | done |
| 2 | Quantity is zero | `{"item":"book","quantity":0}` | `400`, `field` is `quantity` | unit | | done |
| 3 | Quantity is negative | `{"item":"book","quantity":-5}` | `400`, `field` is `quantity` | unit | | to do |
| 4 | Quantity above the maximum | `{"item":"book","quantity":1001}` | `400`, `field` is `quantity` | unit | | to do |
| 5 | Item is empty | `{"item":"","quantity":1}` | `400`, `field` is `item` | unit | | to do |
| 6 | Item is only whitespace | `{"item":"   ","quantity":1}` | `400`, `field` is `item` | unit | | to do |
| 7 | Item is longer than 64 characters | 65-character string | `400`, `field` is `item` | unit | | to do |
| 8 | Item holds non-ASCII characters | `{"item":"کتاب","quantity":1}` | `201`, item preserved exactly | integration | | to do |
| 9 | Required field missing | `{"item":"book"}` | `400`, `field` is `quantity` | unit | | to do |
| 10 | Round trip | create, then `GET /orders/{id}` | `200`, same item and quantity | integration | | to do |
| 11 | Unknown order id | `GET /orders/does-not-exist` | `404`, `error` is `not_found` | integration | | to do |
| 12 | Neighbour's create endpoint | a valid order | schema matches the shared `openapi.yaml` | contract | | to do |
| 13 | Neighbour's validation | `{"item":"book","quantity":0}` | `400` and an `Error` body | contract | | to do |

## Frontend

| # | Scenario | Input | Expected result | Level | Test file | Status |
|---|---|---|---|---|---|---|
| 14 | The order form renders its fields | — | item and quantity inputs present | component | | to do |
| 15 | The form rejects an empty item | submit with an empty item | an error message is shown | component | | to do |
| 16 | A full order journey | fill the form, submit | the new order appears on screen | e2e | | to do |

## Edge cases we deliberately did not cover

| Case | Why not, and when we will |
|---|---|
| | |
