# Authentication & Authorization — Altrium CRM API

Sprint 1 feedback: *"Authentication and authorization must be implemented. A proper
login page for every different actor."* This document describes what was built, how
to switch it on, and what was deliberately left out.

---

## 1. The model in one paragraph

A user proves who they are **once**, at `POST /api/auth/login`, by sending an email
and a password over HTTPS. The API checks the password against a salted PBKDF2 hash
and returns a **signed JSON Web Token (JWT)** holding the user's id and role. Every
later request carries that token in an `Authorization: Bearer …` header. The API
validates the signature on each request — no session table, no database lookup — and
then makes three separate authorization decisions: *are you signed in at all*, *does
your role permit this operation*, and *do you own this particular row*.

---

## 2. Roles

The database `CHECK` constraint on `dbo.[User].user_role` allows three values, and
they map onto the two-tier model from the feature breakdown like this:

| Role | Sees | Can administer users |
|---|---|---|
| `SALES REP` | only rows they own (`user_id` = their id) | no |
| `SALES MANAGER` | everything | no |
| `LEADERSHIP` | everything | yes |

A contact has no owner column of its own, so a rep sees the contacts of the
companies they own.

`Data/Roles.cs` is the single source of truth for these strings and for the
`SeesEverything(role)` rule.

---

## 3. Endpoints

| Endpoint | Who |
|---|---|
| `POST /api/auth/login` | anyone (the only genuinely public endpoint) |
| `POST /api/auth/bootstrap` | anyone, **but only until the first password exists** |
| `GET /api/auth/me` | any signed-in user |
| `POST /api/auth/change-password` | any signed-in user, for their own account |
| `POST /api/auth/register` | LEADERSHIP |
| `POST /api/auth/users/{id}/reset-password` | LEADERSHIP |
| `GET /api/Users`, `GET /api/Users/{id}` | any signed-in user (no password data is returned) |
| `POST` / `PUT` / `DELETE` on `/api/Users` | LEADERSHIP |
| Everything else (Companies, Contact, Leads, Deals, Engagement, FollowUp) | any signed-in user, scoped to what they own |

Anything not marked otherwise requires a valid token: `Program.cs` sets a
`FallbackPolicy` of `RequireAuthenticatedUser`, so **an endpoint with no attribute is
closed, not open**. Forgetting an attribute locks people out instead of leaking data.

### Status codes

| Code | Meaning |
|---|---|
| 401 | no token, or it is expired/invalid — "I do not know who you are" |
| 403 | valid token, wrong role — "I know who you are, and no" |
| 404 | valid token, right role, but not your row — refuses without confirming the record exists |

---

## 4. First-time setup

### 4.1 Database

Run `sql/001_authentication.sql` once against `altrium-crm-db`. It widens
`password_hash` to `NVARCHAR(200)` (the hash format is ~85 characters and a narrower
column silently truncates it), adds a unique index on `email`, blanks the unusable
pre-authentication hashes, and indexes the `user_id` ownership columns.

### 4.2 The signing key

The JWT signing secret must live outside source control. Anyone who has it can mint
a token claiming to be LEADERSHIP.

```bash
# local development (from the Altrium_Project_Backend project folder)
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>"
```

In Azure: **App Service → Configuration → Application settings → `Jwt__Key`**
(double underscore). If the key is missing in a non-Development environment the API
refuses to start, which is deliberate — a silently missing key would mean
unverifiable tokens.

In Development only, a fixed local fallback key is used so the project still runs
straight after a clone.

### 4.3 Creating the first administrator

Every existing row in `dbo.[User]` has an unusable password, so nobody can sign in
yet. `POST /api/auth/bootstrap` covers exactly that gap:

```bash
curl -X POST https://<api>/api/auth/bootstrap \
  -H "Content-Type: application/json" \
  -d '{"name":"Malan","email":"malan@altrium.test","password":"<strong password>","userRole":"LEADERSHIP"}'
```

If the email already exists, that row is given the password and promoted to
LEADERSHIP; otherwise a new user is created. **The endpoint stops working the moment
any account has a usable password**, so it cannot be used to add an administrator to
a live system.

### 4.4 Creating everyone else

Signed in as LEADERSHIP, use `POST /api/auth/register` with a `userRole` of
`SALES REP`, `SALES MANAGER` or `LEADERSHIP`. To give an existing account a password,
use `POST /api/auth/users/{id}/reset-password`.

### 4.5 Trying it in Swagger

Swagger UI has an **Authorize** button. Call `/api/auth/login`, copy the
`accessToken` from the response, paste it in, and the protected endpoints become
callable.

---

## 5. How the front end uses it

| File | Role |
|---|---|
| `src/auth/session.js` | holds the token, ends the session on 401 |
| `src/auth/AuthContext.jsx` | `login()`, `logout()`, restores the session on refresh via `/api/auth/me` |
| `src/auth/RequireAuth.jsx` | route guards: `RequireAuth`, `RequireRole` |
| `src/pages/Login.jsx` | the sign-in screen |
| `src/api/client.js` | attaches `Authorization: Bearer …` to every call |

The token is kept in `sessionStorage`: it survives a page refresh but dies with the
tab. That is a compromise — a token in JavaScript-reachable storage can be read by
any script on the page — and the mitigation is the eight-hour lifetime.

**The route guards are user experience, not security.** They stop a rep being shown
a page full of buttons that would fail anyway. Anyone can open devtools and call the
API directly, which is why every rule is enforced again on the server.

---

## 6. Deliberately not built

| Left out | Why | What stands in for it |
|---|---|---|
| Forgot password by email | needs SMTP/SendGrid, a token table, expiry and rate limiting — the piece most likely to end up half-built and insecure | LEADERSHIP resets the password and passes it on |
| Refresh tokens | a second table plus rotation logic; the value is being able to revoke a session early | an eight-hour access token, then sign in again |
| Per-team scoping for managers | `dbo.[User]` has no `manager_id` or `team_id`, so "their team" is not answerable from the schema | managers see everything, which is what the feature breakdown specifies |
| Account lockout after failed logins | needs an attempt counter per account | failed attempts are logged |

Each of these is a small addition on top of what is now in place, not a redesign.

---

## 7. Known security issues outside this work

1. **`appsettings.json` contains the live Azure SQL admin password and it is in git.**
   Anyone who can read the repository can connect directly to the database, which
   makes API-level authorization irrelevant. Rotate that password, move the
   connection string to an App Service application setting (`ConnectionStrings__Db`),
   and remove it from the file. Note that rotating is necessary even after deleting
   it, because the old value stays in the commit history.
2. **CORS** still allows `http://localhost:5173`. Harmless in development; drop it
   from the production configuration before the final submission.
3. **Swagger is exposed in production.** Convenient for the demo, but it documents
   every endpoint for anyone who finds the URL. Consider wrapping
   `app.UseSwagger()` in an environment check afterwards.
