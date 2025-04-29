# Process Monitor

A comprehensive ASP.NET Core application for monitoring system processes with Entity Framework Core.

## Features

- Real-time process monitoring with detailed information
- Thread tracking for each process
- Alert system for suspicious processes
- Digital signature verification
- Network connections monitoring
- Process history tracking
- RESTful API
- SignalR real-time updates

## Tech Stack

- ASP.NET Core 8.0
- Entity Framework Core 9.0
- SQL Server
- SignalR for real-time updates
- Swagger for API documentation
- System.Management for process information

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQL Server (or LocalDB)
- Visual Studio 2022 or VS Code

### Installation

1. Clone the repository
```
git clone https://github.com/tonyCYuan/ProcessMonitor.git
```

2. Navigate to the project directory
```
cd ProcessMonitor
```

3. Restore dependencies
```
dotnet restore
```

4. Update the database
```
dotnet ef database update
```

5. Run the application
```
dotnet run
```

6. Access the application at `https://localhost:5001` or `http://localhost:5000`

## API Documentation

API documentation is available through Swagger at `/swagger` when running the application.

## License

This project is licensed under the MIT License.