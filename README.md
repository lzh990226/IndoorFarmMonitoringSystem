# Plant Sensor Data API

A .NET 9.0 Web API that aggregates plant sensor data from external services and provides flexible storage options including PostgreSQL, JSON files, and in-memory storage.

## Features

- **Data Aggregation**: Combines sensor readings with plant configurations from external APIs
- **Multiple Storage Options**: 
  - PostgreSQL database (persistent)
  - JSON file storage (persistent)
  - In-memory storage (session-based)
- **RESTful API**: Clean endpoints with optional storage parameters
- **Storage Verification**: Built-in endpoints to verify and manage stored data
- **Swagger Documentation**: Interactive API documentation available

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL (optional, only if using database storage)
- Git

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/lzh990226/IndoorFarmMonitoringSystem.git
cd IndoorFarmMonitoringSystem
```

### 2. Install Dependencies

```bash
cd PlantSensorDataAPI
dotnet restore
```

### 3. Configuration

Update `PlantSensorDataAPI/appsettings.json` with your settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=plant_sensor_db;Username=postgres;Password=your_password"
  },
  "ExternalApis": {
    "SensorReadingsUrl": "http://3.0.148.231:8010/sensor-readings",
    "PlantConfigurationsUrl": "http://3.0.148.231:8020/plant-configurations"
  }
}
```

### 4. Run the Application

```bash
cd PlantSensorDataAPI
dotnet run
```

### 5. Access the API

- **API Base URL**: `http://localhost:5188`
- **Swagger Documentation**: `http://localhost:5188/doc`
- **OpenAPI Spec**: `http://localhost:5188/openapi/v1.json`

## Configuration Options

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Application environment | `Development` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | See appsettings.json |
| `ExternalApis__SensorReadingsUrl` | Sensor readings API endpoint | `http://3.0.148.231:8010/sensor-readings` |
| `ExternalApis__PlantConfigurationsUrl` | Plant configurations API endpoint | `http://3.0.148.231:8020/plant-configurations` |

## API Endpoints

### Core Data Operations

#### Get Plant Sensor Data
```http
GET /plant-sensor-data?storage={storage_type}
```

Retrieves combined sensor readings and plant configurations with optional storage.

**Query Parameters:**
- `storage` (optional): Choose storage method
  - `postgresql` - Save to PostgreSQL database
  - `inmemory` - Save to in-memory storage  
  - `json` - Save to JSON file
  - *omit* - Return data without saving

**Example Responses:**
```json
{
  "success": true,
  "data": [
    {
      "trayId": 1,
      "plantType": "Lettuce",
      "currentTemperature": 25.5,
      "currentHumidity": 60,
      "currentLight": 1000,
      "targetTemperature": 24,
      "targetHumidity": 65,
      "targetLight": 1200,
      "timestamp": "2025-07-27T06:19:26.165944Z"
    }
  ],
  "saved": true,
  "storageType": "inmemory",
  "processingTimeMs": 68
}
```

### Storage Management

#### Verify In-Memory Storage
```http
GET /plant-sensor-data/verify-storage
```

Inspects the current contents of in-memory storage.

**Response:**
```json
{
  "storageType": "InMemory",
  "recordCount": 2,
  "data": [...],
  "timestamp": "2025-07-27T06:19:32.141953Z"
}
```

## Testing

### Run Unit Tests
```bash
cd PlantSensorDataAPI.Tests
dotnet test
```

### Manual API Testing

#### Basic Data Retrieval
```bash
curl -X GET "http://localhost:5188/plant-sensor-data" \
  -H "accept: application/json"
```

#### Save to Different Storage Types
```bash
# PostgreSQL
curl -X GET "http://localhost:5188/plant-sensor-data?storage=postgresql"

# In-Memory
curl -X GET "http://localhost:5188/plant-sensor-data?storage=inmemory"

# JSON File
curl -X GET "http://localhost:5188/plant-sensor-data?storage=json"
```

#### Verify Storage
```bash
# Check in-memory storage
curl -X GET "http://localhost:5188/plant-sensor-data/verify-storage"

# Check JSON file storage
cat PlantSensorDataAPI/Data/plant-sensor-data.json
```

## Database Setup

### PostgreSQL Setup

1. **Install PostgreSQL**
   ```bash
   # macOS with Homebrew
   brew install postgresql
   brew services start postgresql
   
   # Ubuntu/Debian
   sudo apt-get install postgresql postgresql-contrib
   sudo systemctl start postgresql
   sudo systemctl enable postgresql
   ```

2. **Create PostgreSQL User** (Required for macOS Homebrew installations)
   ```bash
   # Create the postgres superuser role if it doesn't exist
   createuser -s postgres
   ```

3. **Create Database**
   ```bash
   # Connect to PostgreSQL and create the database
   psql -d postgres -c "CREATE DATABASE plant_sensor_db;"
   
   # Verify the database was created
   psql -U postgres -d plant_sensor_db -c "SELECT current_database();"
   ```

4. **Update Connection String** in `appsettings.json`
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=plant_sensor_db;Username=postgres;Password="
     }
   }
   ```
   
   **Note**: On macOS with Homebrew, PostgreSQL typically doesn't require a password for local connections.

5. **Run Application** 
   ```bash
   cd PlantSensorDataAPI
   dotnet run
   ```
   
   **Note**: The application will automatically create the database and tables on first run using EF Core's `EnsureCreated()` method.

6. **Test PostgreSQL Storage**
   ```bash
   # Test saving data to PostgreSQL
   curl -X GET "http://localhost:5188/plant-sensor-data?storage=postgresql"
   
   # Verify data was saved to database
   psql -U postgres -d plant_sensor_db -c "SELECT \"TrayId\", \"PlantType\" FROM \"CombinedPlantData\";"
   ```

##  Project Structure

```
IndoorFarmMonitoringSystem/
├── IndoorFarmMonitoringSystem.sln       # Solution file
├── README.md                            # This file
├── PlantSensorDataAPI/                  # Main API project
│   ├──── Controllers/
│   │   └── PlantSensorDataController.cs    # Main API controller
│   ├──── Data/
│   │   ├── PlantSensorDbContext.cs         # EF Core database context
│   │   └── plant-sensor-data.json          # JSON storage (runtime)
│   ├──── Models/
│   │   ├── CombinedPlantData.cs            # Combined data model
│   │   ├── PlantConfiguration.cs           # Plant config model
│   │   └── SensorReading.cs                # Sensor data model
│   ├──── Services/
│   │   ├── IExternalApiService.cs          # External API interface
│   │   ├── ExternalApiService.cs           # External API implementation
│   │   ├── IPlantDataService.cs            # Data service interface
│   │   └── PlantDataService.cs             # Data service implementation
│   ├──── Configuration/
│   │   └── ExternalApiSettings.cs          # API configuration
│   ├──── Properties/
│   │   └── launchSettings.json             # Launch configuration
│   ├──── appsettings.json                 # Application configuration
│   ├──── Program.cs                       # Application entry point
│   └──── PlantSensorDataAPI.csproj        # Project file
└──── PlantSensorDataAPI.Tests/            # Test project
    ├──── Services/
    │   ├── ExternalApiServiceTests.cs      # API service tests
    │   └── PlantDataServiceTests.cs        # Data service tests
    └──── PlantSensorDataAPI.Tests.csproj  # Test project file
```

## Troubleshooting

### Common Issues

**1. External API Connection Failed**
- Verify external API URLs in `appsettings.json`
- Check network connectivity
- Ensure external services are running

**2. PostgreSQL Connection Failed**
- Verify PostgreSQL is running
- Check connection string in `appsettings.json`
- Ensure database exists and user has permissions

**3. Port Already in Use**
- Change port in `PlantSensorDataAPI/Properties/launchSettings.json`
- Kill existing processes

**4. In-Memory Data Lost**
- This is expected behavior when application restarts
- Use JSON or PostgreSQL storage for persistence
