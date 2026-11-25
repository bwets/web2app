# Web2App

Runs web pages as standalone Windows apps. Each app is described by a small `.web2app` file and opens in its own
window, with its own taskbar button and its own browser storage, on top of the Microsoft Edge WebView2 Runtime.

## The `.web2app` file

A YAML file:

```yaml
title: WhatsApp                    # optional: window and taskbar title (default: the page title)
url: https://web.whatsapp.com      # required: start page, http or https
icon: https://example.com/icon.png # optional: absolute or relative to url (default: the page favicon)
storage: personal                  # optional: storage name (default: the file name without extension)
```

Unknown keys are ignored, so files written for newer versions still open.

- **Storage**: apps with the same `storage` name share cookies, logins and site data; any other app is fully isolated.
  Without `storage`, `whatsapp.web2app` uses the storage `whatsapp`, so renaming the file starts a fresh storage.
- **Icon**: ICO, PNG, JPEG, GIF or BMP. When it cannot be downloaded or decoded (SVG for instance), the favicon is used.

## Running

```
Web2App.exe path\to\app.web2app
```

- Launching the same file again brings the running window to the front.
- Popups opened by the page (`window.open`, sign-in flows) open as app windows; links to another site that would open
  a new tab go to the default browser. Navigation inside a window always stays in the app.
- Each file is its own app for the taskbar (AppUserModelID derived from the file path), and pinning a running app
  pins that file.

## Data

Everything lives under `%LOCALAPPDATA%\Web2App`:

| Folder | Content |
|---|---|
| `Storage\<storage>` | WebView2 user data of a storage name |
| `Icons\<app id>.ico` | Last icon of an app, used by the taskbar pin |

## Building

```
dotnet build Web2App.slnx
```

Requires the .NET 10 SDK. `Web2App.Core` holds the file format and paths shared with future tools (editor, shortcut
installer); `Web2App` is the WinForms launcher.
