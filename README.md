# NetFix

**Your internet died again and you don't want to reinstall Windows.** NetFix is a one-click network doctor for Windows: diagnose what's wrong, then run the common fixes with a single button.

No installer, no bloatware, no telemetry of its own. One small WinForms exe, .NET Framework 4.8 (already on every Windows box since 2016).

---

## Why

When the internet goes down, people reach for:

- a heavy "optimizer" suite that installs 5 other things
- the system admin who isn't around
- random forum posts telling them to run 6 commands they can't remember

NetFix replaces all of that. It runs the diagnosis you'd do by hand (ping gateway, ping DNS, resolve a domain, check hosts, check proxy), then applies the fixes everyone actually uses (flush DNS, reset Winsock, reset TCP/IP, clear proxy, clean hosts).

## Features

| | |
|---|---|
| **One-click diagnosis** | Checks IP config, gateway reachability, DNS resolution, actual internet reachability, hosts file, and system proxy — six checks, color-coded output. |
| **One-click repair** | Flushes DNS, resets Winsock and TCP/IP stack, disables a stuck system proxy, and comments out custom hosts entries (with a timestamped backup). |
| **Non-destructive** | Hosts entries are commented out, not deleted — backup saved next to the file. Nothing is hard-removed. |
| **No install** | Single exe, ~17 KB. Run it, fix it, close it. |
| **Admin-aware** | Asks for elevation via manifest; without it, diagnosis still runs but repair steps will report access errors. |

## Quick start

```bash
# build (uses the in-box C# compiler, no Visual Studio)
build.cmd

# run (UAC will ask for admin, needed for repair steps)
bin\NetFix.exe
```

Two big buttons:

1. **Run diagnosis** — see exactly which layer is broken (DNS? gateway? hosts? proxy?).
2. **One-click repair** — applies the common fixes; reboot when it's done.

## What it actually does

Diagnosis:
- IPv4 address and DNS server in use
- Ping default gateway
- Resolve `www.bing.com` via DNS
- HTTPS probe to `msftconnecttest.com` (the same endpoint Windows uses)
- Count custom hosts entries
- Read system proxy setting

Repair:
- `ipconfig /flushdns`
- `netsh winsock reset`
- `netsh int ip reset`
- Set `ProxyEnable = 0` in the registry
- Comment out non-comment lines in `hosts` (backup saved as `hosts.netfix-bak-YYYYMMDD-HHMMSS`)

After repair: reboot. Winsock and TCP resets need a restart.

## Tech

- C# / WinForms, .NET Framework 4.8
- P/Invoke not needed — uses `System.Net.NetworkInformation`, `HttpWebRequest`, `Registry`, and shells out to `ipconfig` / `netsh`
- No third-party dependencies

## Positioning

NetFix is a **user-facing troubleshooting tool**: fix your own box when it can't reach the internet. It doesn't monitor traffic, intercept packets, or touch anything outside the network stack.

Not for: bypassing network policies at work, or anything your sysadmin would not want you to do.

## Layout

```
NetFix/
├── MainForm.cs          # WinForms UI
├── Program.cs
├── Core/
│   ├── NetDiag.cs         # six diagnosis checks
│   └── NetRepair.cs       # five repair steps
├── app.manifest          # requireAdministrator
└── build.cmd
```

## License

[MIT](LICENSE).

---

If NetFix gets you back online without a reinstall, a ⭐ is appreciated.
