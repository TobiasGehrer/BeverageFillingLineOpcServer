using System.Net;
using System.Text.Json;

namespace BeverageFillingLineServer
{
    public class RestApiProgram
    {
        private static BeverageFillingLineMachine s_machine;
        private static HttpListener s_listener;
        private static Timer s_simulationTimer;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("🌐 Starting REST API Beverage Server (Alternative to OPC UA)");

            s_machine = new BeverageFillingLineMachine();

            // Start HTTP server on port 8080
            s_listener = new HttpListener();
            s_listener.Prefixes.Add("http://localhost:8080/");

            try
            {
                s_listener.Start();
                Console.WriteLine("✅ REST API Server started successfully!");
                Console.WriteLine();
                Console.WriteLine("🌐 Access your beverage data via:");
                Console.WriteLine("   http://localhost:8080/data - All machine data");
                Console.WriteLine("   http://localhost:8080/status - Machine status");
                Console.WriteLine("   http://localhost:8080/alarms - Active alarms");
                Console.WriteLine();
                Console.WriteLine("🔍 Open your browser and go to: http://localhost:8080/data");
                Console.WriteLine();

                // Start simulation
                s_simulationTimer = new Timer(UpdateSimulation, null, 1000, 2000);

                // Handle requests
                while (true)
                {
                    var context = await s_listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ REST API Server failed: {ex.Message}");
                Console.WriteLine("This might be an alternative if OPC UA continues to fail.");
                Console.ReadKey();
            }
        }

        private static void UpdateSimulation(object state)
        {
            s_machine.UpdateSimulation();
            Console.WriteLine($"🔄 [{DateTime.Now:HH:mm:ss}] {s_machine.MachineStatus} | " +
                            $"Fill: {s_machine.ActualFillVolume:F1}ml | " +
                            $"Tank: {s_machine.ProductLevelTank:F1}% | " +
                            $"{s_machine.CurrentStation}");

            if (s_machine.ActiveAlarms.Count > 0)
            {
                Console.WriteLine($"⚠️  ALARMS: {string.Join(" | ", s_machine.ActiveAlarms.Take(2))}");
            }
        }

        private static async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                string responseData = "";

                switch (request.Url?.AbsolutePath.ToLower())
                {
                    case "/":
                    case "/index":
                        responseData = GetHtmlDashboard();
                        response.ContentType = "text/html";
                        break;

                    case "/data":
                        responseData = GetAllData();
                        response.ContentType = "application/json";
                        break;

                    case "/status":
                        responseData = JsonSerializer.Serialize(new {
                            MachineStatus = s_machine.MachineStatus,
                            CurrentStation = s_machine.CurrentStation,
                            ProductLevelTank = s_machine.ProductLevelTank
                        });
                        response.ContentType = "application/json";
                        break;

                    case "/alarms":
                        responseData = JsonSerializer.Serialize(new {
                            ActiveAlarms = s_machine.ActiveAlarms,
                            AlarmCount = s_machine.ActiveAlarms.Count
                        });
                        response.ContentType = "application/json";
                        break;

                    default:
                        response.StatusCode = 404;
                        responseData = "Not Found";
                        break;
                }

                var buffer = System.Text.Encoding.UTF8.GetBytes(responseData);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Request error: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
        }

        private static string GetAllData()
        {
            var data = new
            {
                // Machine Identification
                MachineName = s_machine.MachineName,
                MachineSerialNumber = s_machine.MachineSerialNumber,
                Plant = s_machine.Plant,
                ProductionSegment = s_machine.ProductionSegment,
                ProductionLine = s_machine.ProductionLine,

                // Production Order
                ProductionOrder = s_machine.ProductionOrder,
                Article = s_machine.Article,
                Quantity = s_machine.Quantity,
                CurrentLotNumber = s_machine.CurrentLotNumber,
                ExpirationDate = s_machine.ExpirationDate,

                // Target Values
                TargetFillVolume = s_machine.TargetFillVolume,
                TargetLineSpeed = s_machine.TargetLineSpeed,
                TargetProductTemperature = s_machine.TargetProductTemperature,
                TargetCO2Pressure = s_machine.TargetCO2Pressure,
                TargetCapTorque = s_machine.TargetCapTorque,
                TargetCycleTime = s_machine.TargetCycleTime,

                // Actual Values
                ActualFillVolume = s_machine.ActualFillVolume,
                ActualLineSpeed = s_machine.ActualLineSpeed,
                ActualProductTemperature = s_machine.ActualProductTemperature,
                ActualCO2Pressure = s_machine.ActualCO2Pressure,
                ActualCapTorque = s_machine.ActualCapTorque,
                ActualCycleTime = s_machine.ActualCycleTime,
                FillAccuracyDeviation = s_machine.FillAccuracyDeviation,

                // System Status
                MachineStatus = s_machine.MachineStatus,
                CurrentStation = s_machine.CurrentStation,
                ProductLevelTank = s_machine.ProductLevelTank,
                CleaningCycleStatus = s_machine.CleaningCycleStatus,
                QualityCheckWeight = s_machine.QualityCheckWeight,
                QualityCheckLevel = s_machine.QualityCheckLevel,

                // Counters
                GoodBottles = s_machine.GoodBottles,
                BadBottlesVolume = s_machine.BadBottlesVolume,
                BadBottlesWeight = s_machine.BadBottlesWeight,
                BadBottlesCap = s_machine.BadBottlesCap,
                BadBottlesOther = s_machine.BadBottlesOther,
                TotalBadBottles = s_machine.TotalBadBottles,
                TotalBottles = s_machine.TotalBottles,
                GoodBottlesOrder = s_machine.GoodBottlesOrder,
                BadBottlesOrder = s_machine.BadBottlesOrder,
                TotalBottlesOrder = s_machine.TotalBottlesOrder,
                ProductionOrderProgress = s_machine.ProductionOrderProgress,

                // Alarms
                ActiveAlarms = s_machine.ActiveAlarms,

                // Timestamp
                LastUpdate = DateTime.Now
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        }

        private static string GetHtmlDashboard()
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Beverage Filling Line Dashboard</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }}
        .container {{ max-width: 1200px; margin: 0 auto; }}
        .card {{ background: white; padding: 20px; margin: 10px 0; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .status-running {{ color: green; font-weight: bold; }}
        .status-error {{ color: red; font-weight: bold; }}
        .grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; }}
        .metric {{ padding: 10px; background: #f8f9fa; border-left: 4px solid #007bff; margin: 5px 0; }}
        .alarm {{ background: #fff3cd; border-left: 4px solid #ffc107; color: #856404; }}
        h1 {{ color: #333; text-align: center; }}
        h2 {{ color: #555; border-bottom: 2px solid #ddd; padding-bottom: 10px; }}
    </style>
    <script>
        setInterval(() => location.reload(), 5000);
    </script>
</head>
<body>
    <div class='container'>
        <h1>🏭 {s_machine.MachineName} - Live Dashboard</h1>

        <div class='card'>
            <h2>📊 Current Status</h2>
            <div class='status-{(s_machine.MachineStatus == "Running" ? "running" : "error")}'>
                Status: {s_machine.MachineStatus}
            </div>
            <div>Station: {s_machine.CurrentStation}</div>
            <div>Tank Level: {s_machine.ProductLevelTank:F1}%</div>
        </div>

        <div class='grid'>
            <div class='card'>
                <h2>🥤 Fill Data</h2>
                <div class='metric'>Actual: {s_machine.ActualFillVolume:F1} ml</div>
                <div class='metric'>Target: {s_machine.TargetFillVolume:F0} ml</div>
                <div class='metric'>Deviation: {s_machine.FillAccuracyDeviation:F1} ml</div>
            </div>

            <div class='card'>
                <h2>⚡ Production</h2>
                <div class='metric'>Speed: {s_machine.ActualLineSpeed:F0} BPM</div>
                <div class='metric'>Good Bottles: {s_machine.GoodBottles:N0}</div>
                <div class='metric'>Progress: {s_machine.ProductionOrderProgress:F1}%</div>
            </div>
        </div>

        {(s_machine.ActiveAlarms.Count > 0 ?
            $@"<div class='card'>
                <h2>⚠️ Active Alarms ({s_machine.ActiveAlarms.Count})</h2>
                {string.Join("", s_machine.ActiveAlarms.Take(5).Select(a => $"<div class='alarm'>{a}</div>"))}
            </div>" : "")}

        <div class='card'>
            <h2>🔗 API Endpoints</h2>
            <p><a href='/data'>📊 /data</a> - Complete machine data (JSON)</p>
            <p><a href='/status'>📈 /status</a> - Status information</p>
            <p><a href='/alarms'>⚠️ /alarms</a> - Active alarms</p>
        </div>

        <div style='text-align: center; color: #666; margin-top: 20px;'>
            🔄 Auto-refreshes every 5 seconds | Last update: {DateTime.Now:HH:mm:ss}
        </div>
    </div>
</body>
</html>";
        }
    }
}