# Tetris WPF

A responsive Tetris game built with WPF (Windows Presentation Foundation) featuring asynchronous operations and dynamic UI scaling.

## Features

### InputDialog Component
The InputDialog is a custom asynchronous input dialog component that replaces the blocking `Microsoft.VisualBasic.Interaction.InputBox`. It ensures the UI thread remains responsive during user input, preventing the application from freezing. The dialog uses async/await pattern with `Dispatcher.InvokeAsync` to maintain WPF responsiveness best practices.

## Database Setup

Use these commands to initialize your SQL SERVER DB.

Add-Migration InitialCreate
Update-Database
