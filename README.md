# MinecraftTracker

**MinecraftTracker** is a Windows desktop application developed to monitor game activity and record play sessions automatically. The project currently focuses on Minecraft and was designed as a portfolio project to demonstrate desktop development, process monitoring, UI design, data persistence, and clean project organization.

## Overview

The application detects whether Minecraft is running on the computer and starts a session timer automatically. When the game is closed, the session duration can be stored for later consultation.

The dashboard presents the current game status, the elapsed time of the active session, accumulated playtime and recent sessions in a visual interface with a dark, minimal and gamer-oriented design.

## Main Features

- Automatic detection of Minecraft processes.
- Real-time tracking of the current session.
- Automatic calculation of total playtime.
- Daily playtime indicator.
- Recent session history.
- Persistent session storage using SQLite through the project's database service.
- Custom Windows title bar and application icon.
- Dark and minimalist gamer-oriented interface.
- Navigation structure prepared for future statistics and settings screens.

## Technologies

- C#
- .NET / WinUI 3
- XAML
- SQLite
- Visual Studio
- Git / GitHub

## Project Architecture

The current implementation separates the visual layer from the tracking logic:

- `MainWindow.xaml` — application interface and visual resources.
- `MainWindow.xaml.cs` — window configuration, UI event handlers and integration with the view model.
- `MainViewModel` — game monitoring, session timer, dashboard data and property notifications.
- `DatabaseService` — persistence and retrieval of session data.

This structure was chosen to keep UI changes independent from the core tracking behavior as much as possible.

## How It Works

1. The application starts and initializes the main window.
2. The dashboard loads previously stored statistics and sessions.
3. A background loop checks for Minecraft-related processes.
4. When Minecraft is detected, the session timer starts.
5. While the game is running, the elapsed time is updated continuously.
6. When Minecraft is no longer detected, the session is finalized.
7. Sessions that meet the minimum duration configured in the tracking logic are saved to the database.
8. Dashboard statistics and recent sessions are refreshed.

## Current Scope

The first version is intentionally focused on Minecraft. The project structure leaves room for future support for other games, richer statistics, configuration options, charts, and additional dashboard pages.

## Portfolio Goals

MinecraftTracker was created not only as a utility, but also as a practical software development project. It demonstrates the ability to work with:

- Desktop application development.
- Event-driven UI development.
- Process detection in Windows.
- Asynchronous programming.
- Data binding and property notification.
- Local data persistence.
- UI/UX organization.
- Git and GitHub project documentation.

## Roadmap

- [ ] Add detailed statistics by day, week and month.
- [ ] Add charts for playtime evolution.
- [ ] Support multiple games.
- [ ] Add game configuration management.
- [ ] Improve session history filtering.
- [ ] Add settings page functionality.
- [ ] Add more robust process identification for different Minecraft launchers and editions.

## Project Status

**In development.**

The current version establishes the main tracking flow and dashboard foundation. New features are planned as the project evolves.
