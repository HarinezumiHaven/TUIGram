# 🚀 TUIGram

<div align="center">

**A modern Terminal User Interface (TUI) Telegram client**

*Experience Telegram right from your terminal with a sleek, keyboard-driven interface*

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Linux%20%7C%20Windows%20%7C%20macOS-lightgrey?style=flat-square)](#installation)

</div>

## ✨ Features

- 📱 **Telegram functionality** in your terminal
- ⚡ **Lightning fast** - no GUI overhead
- 🔒 **Secure** - uses official Telegram API
- 🌙 **Terminal-native** - works great with your existing workflow
- 💻 **Cross-platform** - Linux, Windows, macOS support

## 🚀 Quick Start

### Prerequisites

Before you begin, you'll need:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- A Telegram account
- API credentials from [my.telegram.org/apps](https://my.telegram.org/apps)

### Installation

#### Option 1: Run from Source (Recommended for development)
```bash
# Clone the repository
git clone https://github.com/HarinezumiHaven/TUIGram.git

# Navigate to the project directory
cd TUIGram

# Restore dependencies and run
dotnet restore
dotnet run
```

#### Option 2: Build Executable
```bash
# Build for your current platform
dotnet build -c Release

# Or create a self-contained executable
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

#### Option 3: For NixOS Users
```bash
# Using Nix flakes (if available)
nix run github:HarinezumiHaven/TUIGram

# Or build locally
nix-build
./result/bin/tuigram
```

## ⚙️ Setup Guide

1. **Get Telegram API Credentials**
   - Visit [my.telegram.org/apps](https://my.telegram.org/apps)
   - Log in with your Telegram account
   - Create a new application
   - Note down your `api_id` and `api_hash`

2. **First Run Setup**
   ```bash
   dotnet run
   ```
   
3. **Enter Your Credentials**
   - Input your `api_id` (numeric value)
   - Input your `api_hash` (string value)
   - Enter your phone number with country code (e.g., `+1234567890`)
   - Enter the confirmation code sent to your Telegram app

4. **Start Chatting!** 🎉

## 🎮 Usage

Once authenticated, you can:
- Navigate chats with arrow keys
- Send messages by typing and pressing Enter
- Use keyboard shortcuts for quick actions
- Access settings and options through the interface

## 🔧 Development

### Building from Source
```bash
# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run in debug mode
dotnet run

```


## 📋 System Requirements

- **.NET 8.0 Runtime** or SDK
- **Terminal** with Unicode support (recommended)
- **Internet connection** for Telegram API
- **Modern terminal emulator** for best experience

## 🐛 Troubleshooting

### Common Issues

**"dotnet command not found"**
- Install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

**Authentication fails**
- Double-check your `api_id` and `api_hash` from [my.telegram.org/apps](https://my.telegram.org/apps)
- Ensure your phone number includes the country code

**Terminal display issues**
- Use a modern terminal with Unicode support
- Ensure your terminal size is at least 80x24

## 🤝 Contributing

Contributions are welcome! Here's how you can help:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Telegram](https://telegram.org/) for providing the API
- [.NET](https://dotnet.microsoft.com/) team for the excellent runtime
- All contributors who help improve this project

---

<div align="center">

**Made with ❤️ for the terminal enthusiasts**

[Report Bug](https://github.com/HarinezumiHaven/TUIGram/issues) · [Request Feature](https://github.com/HarinezumiHaven/TUIGram/issues) · [Documentation](https://github.com/HarinezumiHaven/TUIGram/wiki)

</div>