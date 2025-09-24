namespace BeverageFillingLineServer
{
    public class BeverageFillingLineMachine
    {
        private Random m_random = new Random();
        private int m_cycleCounter = 0;

        // Process parameters
        public double TargetFillVolume { get; set; } = 1000.0;
        public double ActualFillVolume { get; set; } = 999.2;
        public double TargetLineSpeed { get; set; } = 450.0;
        public double ActualLineSpeed { get; set; } = 448.0;
        public double TargetProductTemperature { get; set; } = 6.5;
        public double ActualProductTemperature { get; set; } = 6.3;
        public double FillDeviation { get; set; } = -0.8;

        // System status
        public string MachineStatus { get; set; } = "Running";
        public string CurrentStation { get; set; } = "Station 12";
        public double ProductLevelTank { get; set; } = 67.3;

        // Counters
        public uint GoodBottles { get; set; } = 1247589;
        public uint TotalBottles { get; set; } = 1254249;

        public void UpdateSimulation()
        {
            if (MachineStatus != "Running")
            {
                return;
            }

            m_cycleCounter++;

            // Simulate realistic variations
            ActualFillVolume = TargetFillVolume + (m_random.NextDouble() - 0.5) * 4.0;
            ActualLineSpeed = TargetLineSpeed + (m_random.NextDouble() - 0.5) * 10.0;
            ActualProductTemperature = TargetProductTemperature + (m_random.NextDouble() - 0.5) * 1.0;

            FillDeviation = ActualFillVolume - TargetFillVolume;
            ProductLevelTank = Math.Max(5.0, ProductLevelTank - 0.01);

            // Rotate stations
            int stationNumber = (m_cycleCounter % 16) + 1;
            CurrentStation = $"Station {stationNumber}";

            // Update counters
            if (m_cycleCounter % 10 == 0)
            {
                GoodBottles++;
                TotalBottles++;
            }

            CheckAlarms();
        }

        private void CheckAlarms()
        {
            // Fill volume alarm: > +-1%
            if (Math.Abs(FillDeviation / TargetFillVolume * 100) > 1.0)
            {
                MachineStatus = "Error";
                Console.WriteLine($"ALARM: Fill deviation {FillDeviation:F2}ml exceeds +-1%");
                return;
            }

            // Temperature alarm: > +-2°C
            if (Math.Abs(ActualProductTemperature - TargetProductTemperature) > 2.0)
            {
                MachineStatus = "Error";
                Console.WriteLine($"ALARM: Product temperature {ActualProductTemperature:F1}°C exceeds +-2°C");
                return;
            }

            // Tank alarm: < 10%
            if (ProductLevelTank < 10.0)
            {
                MachineStatus = "Error";
                Console.WriteLine($"ALARM: Tank level {ProductLevelTank:F1}% too low");
                return;
            }
        }

        public void StartMachine()
        {
            if (MachineStatus == "Stopped")
            {
                MachineStatus = "Starting";
                Task.Delay(3000).ContinueWith(_ => MachineStatus = "Running");
                Console.WriteLine("Machine starting...");
            }
        }

        public void StopMachine()
        {
            if (MachineStatus == "Running" || MachineStatus == "Error")
            {
                MachineStatus = "Stopping";
                Task.Delay(3000).ContinueWith(_ => MachineStatus = "Stopped");
                Console.WriteLine("Machine stopping...");
            }
        }
    }
}
