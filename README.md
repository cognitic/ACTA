# ACTA : ADLDS Connection Tester Attempt

A lightweight Windows console utility to validate **Active Directory Lightweight Directory Services (AD LDS)**
connection settings directly on a target server, before deploying or troubleshooting an application that depends on it.

---

## Purpose

When configuring an application that connects to an AD LDS instance, it can be difficult to know whether
the connection settings (host, container, credentials) are correct without running the full application.

This tool wraps `PrincipalContext` from `System.DirectoryServices.AccountManagement` and exposes
three isolated test scenarios that can be toggled via `App.config`, allowing you to validate each
operation independently and quickly identify misconfigurations.

---

## Use Cases

| Test | What it validates |
|---|---|
| **Search User** | The connection is reachable and the service account can query the directory |
| **Change Password** | Credentials are valid and the service account has write permissions |
| **Create Account** | The service account has sufficient rights to provision new users |

---

## Requirements

- Windows Server (AD LDS is Windows-only)
- .NET Framework 4.0 or higher
- Access to the target AD LDS instance from the server where the tool is run
- A service account with appropriate AD LDS permissions

---

## Configuration

All settings are defined in `App.config`. Toggle each test by setting its `enabled` flag to `true`.

```xml
<!-- AD LDS Connection -->
<add key="adlds:host"      value="LOCALHOST:00000"/>
<add key="adlds:container" value="OU=Users,O=MyApp"/>
<add key="adlds:username"  value="DOMAIN\serviceaccount"/>
<add key="adlds:password"  value="***********"/>

<!-- Test: Search User -->
<add key="test:searchUser:enabled"  value="false"/>
<add key="test:searchUser:username" value="user@domain.com"/>

<!-- Test: Change Password -->
<add key="test:changePassword:enabled"     value="false"/>
<add key="test:changePassword:username"    value="user@domain.com"/>
<add key="test:changePassword:oldPassword" value="***********"/>
<add key="test:changePassword:newPassword" value="***********"/>

<!-- Test: Create Account -->
<add key="test:createAccount:enabled"  value="false"/>
<add key="test:createAccount:username" value="newuser@domain.com"/>
<add key="test:createAccount:password" value="***********"/>
```

> ⚠️ Never commit `App.config` containing real credentials to source control.  
> Use a `.gitignore` rule or a secrets management approach for sensitive values.

---

## Usage

1. Copy the build output to the target server
2. Edit `App.config` with the AD LDS connection settings for that environment
3. Set the desired test flags to `true`
4. Run the executable from a command prompt

```bash
AdldsConnectionTester.exe
```

Each enabled test prints its result to the console:
