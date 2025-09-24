using Opc.Ua;
using Opc.Ua.Server;

namespace BeverageFillingLineServer
{
    public class BeverageFillingLineNodeManager : CustomNodeManager2
    {
        private BeverageFillingLineMachine m_machine;
        private Dictionary<string, BaseDataVariableState> m_variables;
        private Timer m_simulationTimer;

        public BeverageFillingLineNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration, "http://fluidfill.com/machine/")
        {
            m_variables = new Dictionary<string, BaseDataVariableState>();
            SetNamespaces("http://fluidfill.com/machine/");
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);

                // Start simulation timer (update every 2 seconds)
                m_simulationTimer = new Timer(OnSimulationTimer, null, 2000, 2000);
            }
        }

        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();
            m_machine = new BeverageFillingLineMachine();

            // Root machine folder
            FolderState root = new FolderState(null)
            {
                NodeId = new NodeId("FluidFillExpress2", NamespaceIndex),
                BrowseName = new QualifiedName("FluidFill Express #2", NamespaceIndex),
                DisplayName = new LocalizedText("en", "FluidFill Express #2"),
                TypeDefinitionId = ObjectTypeIds.FolderType
            };

            root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
            AddPredefinedNode(context, root);
            predefinedNodes.Add(root);

            // Create hierarchical structure
            var identification = CreateFolder(root, "Identification", "Machine Identification");
            var productionOrder = CreateFolder(root, "ProductionOrder", "Production Order");
            var targetValues = CreateFolder(root, "TargetValues", "Target Values");
            var actualValues = CreateFolder(root, "ActualValues", "Actual Values");
            var systemStatus = CreateFolder(root, "SystemStatus", "System Status");
            var counters = CreateFolder(root, "Counters", "Counters");
            var quality = CreateFolder(root, "Quality", "Quality Control");

            predefinedNodes.Add(identification);
            predefinedNodes.Add(productionOrder);
            predefinedNodes.Add(targetValues);
            predefinedNodes.Add(actualValues);
            predefinedNodes.Add(systemStatus);
            predefinedNodes.Add(counters);
            predefinedNodes.Add(quality);

            // Machine Identification
            CreateVariable(identification, "MachineName", "Machine Name", DataTypeIds.String, ValueRanks.Scalar, m_machine.MachineName, predefinedNodes);
            CreateVariable(identification, "MachineSerialNumber", "Machine Serial Number", DataTypeIds.String, ValueRanks.Scalar, m_machine.MachineSerialNumber, predefinedNodes);
            CreateVariable(identification, "Plant", "Plant", DataTypeIds.String, ValueRanks.Scalar, m_machine.Plant, predefinedNodes);
            CreateVariable(identification, "ProductionSegment", "Production Segment", DataTypeIds.String, ValueRanks.Scalar, m_machine.ProductionSegment, predefinedNodes);
            CreateVariable(identification, "ProductionLine", "Production Line", DataTypeIds.String, ValueRanks.Scalar, m_machine.ProductionLine, predefinedNodes);

            // Production Order
            CreateVariable(productionOrder, "ProductionOrder", "Production Order", DataTypeIds.String, ValueRanks.Scalar, m_machine.ProductionOrder, predefinedNodes);
            CreateVariable(productionOrder, "Article", "Article", DataTypeIds.String, ValueRanks.Scalar, m_machine.Article, predefinedNodes);
            CreateVariable(productionOrder, "Quantity", "Quantity", DataTypeIds.UInt32, ValueRanks.Scalar, m_machine.Quantity, predefinedNodes);
            CreateVariable(productionOrder, "CurrentLotNumber", "Current Lot Number", DataTypeIds.String, ValueRanks.Scalar, m_machine.CurrentLotNumber, predefinedNodes);
            CreateVariable(productionOrder, "ExpirationDate", "Expiration Date", DataTypeIds.DateTime, ValueRanks.Scalar, m_machine.ExpirationDate, predefinedNodes);

            // Target Values
            CreateVariable(targetValues, "TargetFillVolume", "Target Fill Volume", DataTypeIds.Double, ValueRanks.Scalar, m_machine.TargetFillVolume, predefinedNodes);
            CreateVariable(targetValues, "TargetLineSpeed", "Target Line Speed", DataTypeIds.Double, ValueRanks.Scalar, m_machine.TargetLineSpeed, predefinedNodes);
            CreateVariable(targetValues, "TargetProductTemperature", "Target Product Temperature", DataTypeIds.Double, ValueRanks.Scalar, m_machine.TargetProductTemperature, predefinedNodes);
            CreateVariable(targetValues, "TargetCO2Pressure", "Target CO2 Pressure", DataTypeIds.Double, ValueRanks.Scalar, m_machine.TargetCO2Pressure, predefinedNodes);
            CreateVariable(targetValues, "TargetCapTorque", "Target Cap Torque", DataTypeIds.Double, ValueRanks.Scalar, m_machine.TargetCapTorque, predefinedNodes);
            CreateVariable(targetValues, "TargetCycleTime", "Target Cycle Time", DataTypeIds.Double, ValueRanks.Scalar, m_machine.TargetCycleTime, predefinedNodes);

            // Actual Values
            CreateVariable(actualValues, "ActualFillVolume", "Actual Fill Volume", DataTypeIds.Double, ValueRanks.Scalar, m_machine.ActualFillVolume, predefinedNodes);
            CreateVariable(actualValues, "ActualLineSpeed", "Actual Line Speed", DataTypeIds.Double, ValueRanks.Scalar, m_machine.ActualLineSpeed, predefinedNodes);
            CreateVariable(actualValues, "ActualProductTemperature", "Actual Product Temperature", DataTypeIds.Double, ValueRanks.Scalar, m_machine.ActualProductTemperature, predefinedNodes);
            CreateVariable(actualValues, "ActualCO2Pressure", "Actual CO2 Pressure", DataTypeIds.Double, ValueRanks.Scalar, m_machine.ActualCO2Pressure, predefinedNodes);
            CreateVariable(actualValues, "ActualCapTorque", "Actual Cap Torque", DataTypeIds.Double, ValueRanks.Scalar, m_machine.ActualCapTorque, predefinedNodes);
            CreateVariable(actualValues, "ActualCycleTime", "Actual Cycle Time", DataTypeIds.Double, ValueRanks.Scalar, m_machine.ActualCycleTime, predefinedNodes);
            CreateVariable(actualValues, "FillAccuracyDeviation", "Fill Accuracy Deviation", DataTypeIds.Double, ValueRanks.Scalar, m_machine.FillAccuracyDeviation, predefinedNodes);

            // System Status
            CreateVariable(systemStatus, "MachineStatus", "Machine Status", DataTypeIds.String, ValueRanks.Scalar, m_machine.MachineStatus, predefinedNodes);
            CreateVariable(systemStatus, "CurrentStation", "Current Station", DataTypeIds.String, ValueRanks.Scalar, m_machine.CurrentStation, predefinedNodes);
            CreateVariable(systemStatus, "ProductLevelTank", "Product Level Tank", DataTypeIds.Double, ValueRanks.Scalar, m_machine.ProductLevelTank, predefinedNodes);
            CreateVariable(systemStatus, "CleaningCycleStatus", "Cleaning Cycle Status", DataTypeIds.String, ValueRanks.Scalar, m_machine.CleaningCycleStatus, predefinedNodes);

            // Quality Control
            CreateVariable(quality, "QualityCheckWeight", "Quality Check Weight", DataTypeIds.String, ValueRanks.Scalar, m_machine.QualityCheckWeight, predefinedNodes);
            CreateVariable(quality, "QualityCheckLevel", "Quality Check Level", DataTypeIds.String, ValueRanks.Scalar, m_machine.QualityCheckLevel, predefinedNodes);

            // Counters - Global
            var globalCounters = CreateFolder(counters, "Global", "Global Counters");
            predefinedNodes.Add(globalCounters);
            CreateVariable(globalCounters, "GoodBottles", "Good Bottles", DataTypeIds.UInt32, ValueRanks.Scalar, m_machine.GoodBottles, predefinedNodes);
            CreateVariable(globalCounters, "BadBottlesVolume", "Bad Bottles Volume", DataTypeIds.UInt32, ValueRanks.Scalar, m_machine.BadBottlesVolume, predefinedNodes);
            CreateVariable(globalCounters, "BadBottlesWeight", "Bad Bottles Weight", DataTypeIds.UInt32, ValueRanks.Scalar, m_machine.BadBottlesWeight, predefinedNodes);
            CreateVariable(globalCounters, "BadBottlesCap", "Bad Bottles Cap", DataTypeIds.UInt32, ValueRanks.Scalar, m_machine.BadBottlesCap, predefinedNodes);
            CreateVariable(globalCounters, "BadBottlesOther", "Bad Bottles Other", DataTypeIds.UInt32, ValueRanks.Scalar, m_machine.BadBottlesOther, predefinedNodes);
            CreateVariable(globalCounters, "TotalBadBottles", "Total Bad Bottles", DataTypeIds.UInt32, ValueRanks.Scalar, m_machine.TotalBadBottles, predefinedNodes);
            CreateVariable(globalCounters, "TotalBottles", "Total Bottles", DataTypeIds.UInt32, ValueRanks.Scalar, m_machine.TotalBottles, predefinedNodes);

            // Counters - Order
            var orderCounters = CreateFolder(counters, "Order", "Order Counters");
            predefinedNodes.Add(orderCounters);
            CreateVariable(orderCounters, "GoodBottlesOrder", "Good Bottles Order", DataTypeIds.UInt32, ValueRanks.Scalar, m_machine.GoodBottlesOrder, predefinedNodes);
            CreateVariable(orderCounters, "BadBottlesOrder", "Bad Bottles Order", DataTypeIds.UInt32, ValueRanks.Scalar, m_machine.BadBottlesOrder, predefinedNodes);
            CreateVariable(orderCounters, "TotalBottlesOrder", "Total Bottles Order", DataTypeIds.UInt32, ValueRanks.Scalar, m_machine.TotalBottlesOrder, predefinedNodes);
            CreateVariable(orderCounters, "ProductionOrderProgress", "Production Order Progress", DataTypeIds.Double, ValueRanks.Scalar, m_machine.ProductionOrderProgress, predefinedNodes);

            // Add alarm variable
            CreateVariable(systemStatus, "ActiveAlarms", "Active Alarms", DataTypeIds.String, ValueRanks.OneDimension, m_machine.ActiveAlarms.ToArray(), predefinedNodes);

            // Create methods folder and add all methods
            var methods = CreateFolder(root, "Methods", "Control Methods");
            predefinedNodes.Add(methods);
            CreateMethods(methods, predefinedNodes);

            return predefinedNodes;
        }

        private void OnSimulationTimer(object state)
        {
            try
            {
                lock (Lock)
                {
                    // Update machine simulation
                    m_machine.UpdateSimulation();

                    // Update all OPC UA variables with current values
                    UpdateVariables();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Simulation Error: {ex.Message}");
            }
        }

        private void UpdateVariables()
        {
            // Machine Identification (static, no updates needed)

            // Production Order
            UpdateVariable("ProductionOrder", m_machine.ProductionOrder);
            UpdateVariable("Article", m_machine.Article);
            UpdateVariable("Quantity", m_machine.Quantity);
            UpdateVariable("CurrentLotNumber", m_machine.CurrentLotNumber);
            UpdateVariable("ExpirationDate", m_machine.ExpirationDate);

            // Target Values (updated when production order changes)
            UpdateVariable("TargetFillVolume", m_machine.TargetFillVolume);
            UpdateVariable("TargetLineSpeed", m_machine.TargetLineSpeed);
            UpdateVariable("TargetProductTemperature", m_machine.TargetProductTemperature);
            UpdateVariable("TargetCO2Pressure", m_machine.TargetCO2Pressure);
            UpdateVariable("TargetCapTorque", m_machine.TargetCapTorque);
            UpdateVariable("TargetCycleTime", m_machine.TargetCycleTime);

            // Actual Values (continuously updated)
            UpdateVariable("ActualFillVolume", m_machine.ActualFillVolume);
            UpdateVariable("ActualLineSpeed", m_machine.ActualLineSpeed);
            UpdateVariable("ActualProductTemperature", m_machine.ActualProductTemperature);
            UpdateVariable("ActualCO2Pressure", m_machine.ActualCO2Pressure);
            UpdateVariable("ActualCapTorque", m_machine.ActualCapTorque);
            UpdateVariable("ActualCycleTime", m_machine.ActualCycleTime);
            UpdateVariable("FillAccuracyDeviation", m_machine.FillAccuracyDeviation);

            // System Status
            UpdateVariable("MachineStatus", m_machine.MachineStatus);
            UpdateVariable("CurrentStation", m_machine.CurrentStation);
            UpdateVariable("ProductLevelTank", m_machine.ProductLevelTank);
            UpdateVariable("CleaningCycleStatus", m_machine.CleaningCycleStatus);

            // Quality Control
            UpdateVariable("QualityCheckWeight", m_machine.QualityCheckWeight);
            UpdateVariable("QualityCheckLevel", m_machine.QualityCheckLevel);

            // Global Counters
            UpdateVariable("GoodBottles", m_machine.GoodBottles);
            UpdateVariable("BadBottlesVolume", m_machine.BadBottlesVolume);
            UpdateVariable("BadBottlesWeight", m_machine.BadBottlesWeight);
            UpdateVariable("BadBottlesCap", m_machine.BadBottlesCap);
            UpdateVariable("BadBottlesOther", m_machine.BadBottlesOther);
            UpdateVariable("TotalBadBottles", m_machine.TotalBadBottles);
            UpdateVariable("TotalBottles", m_machine.TotalBottles);

            // Order Counters
            UpdateVariable("GoodBottlesOrder", m_machine.GoodBottlesOrder);
            UpdateVariable("BadBottlesOrder", m_machine.BadBottlesOrder);
            UpdateVariable("TotalBottlesOrder", m_machine.TotalBottlesOrder);
            UpdateVariable("ProductionOrderProgress", m_machine.ProductionOrderProgress);

            // Active Alarms
            UpdateVariable("ActiveAlarms", m_machine.ActiveAlarms.ToArray());
        }

        private void UpdateVariable(string path, object value)
        {
            if (m_variables.ContainsKey(path))
            {
                m_variables[path].Value = value;
                m_variables[path].Timestamp = DateTime.UtcNow;
                m_variables[path].ClearChangeMasks(SystemContext, false);
            }
        }

        private FolderState CreateFolder(NodeState parent, string path, string name)
        {
            FolderState folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            if (parent != null)
            {
                parent.AddChild(folder);
            }

            return folder;
        }

        private BaseDataVariableState CreateVariable(NodeState parent, string path, string name, NodeId dataType, int valueRank, object initialValue, NodeStateCollection predefinedNodes)
        {
            BaseDataVariableState variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                DataType = dataType,
                ValueRank = valueRank,
                AccessLevel = AccessLevels.CurrentRead,
                UserAccessLevel = AccessLevels.CurrentRead,
                Historizing = false,
                Value = initialValue,
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            m_variables[path] = variable;
            predefinedNodes.Add(variable);
            return variable;
        }

        private void CreateMethods(FolderState parent, NodeStateCollection predefinedNodes)
        {
            // Start Machine Method
            var startMethod = CreateMethod(parent, "StartMachine", "Start Machine", OnStartMachine);
            predefinedNodes.Add(startMethod);

            // Stop Machine Method
            var stopMethod = CreateMethod(parent, "StopMachine", "Stop Machine", OnStopMachine);
            predefinedNodes.Add(stopMethod);

            // Load Production Order Method
            var loadOrderMethod = CreateMethodWithArgs(parent, "LoadProductionOrder", "Load Production Order", OnLoadProductionOrder,
                new[] {
                    ("OrderNumber", DataTypeIds.String),
                    ("Article", DataTypeIds.String),
                    ("Quantity", DataTypeIds.UInt32),
                    ("TargetFillVolume", DataTypeIds.Double),
                    ("TargetLineSpeed", DataTypeIds.Double),
                    ("TargetProductTemp", DataTypeIds.Double),
                    ("TargetCO2Pressure", DataTypeIds.Double),
                    ("TargetCapTorque", DataTypeIds.Double),
                    ("TargetCycleTime", DataTypeIds.Double)
                });
            predefinedNodes.Add(loadOrderMethod);

            // Enter Maintenance Mode
            var maintenanceMethod = CreateMethod(parent, "EnterMaintenanceMode", "Enter Maintenance Mode", OnEnterMaintenanceMode);
            predefinedNodes.Add(maintenanceMethod);

            // Start CIP Cycle
            var cipMethod = CreateMethod(parent, "StartCIPCycle", "Start CIP Cycle", OnStartCIPCycle);
            predefinedNodes.Add(cipMethod);

            // Start SIP Cycle
            var sipMethod = CreateMethod(parent, "StartSIPCycle", "Start SIP Cycle", OnStartSIPCycle);
            predefinedNodes.Add(sipMethod);

            // Reset Counters
            var resetMethod = CreateMethod(parent, "ResetCounters", "Reset Counters", OnResetCounters);
            predefinedNodes.Add(resetMethod);

            // Change Product
            var changeProductMethod = CreateMethodWithArgs(parent, "ChangeProduct", "Change Product", OnChangeProduct,
                new[] {
                    ("NewArticle", DataTypeIds.String),
                    ("NewTargetFillVolume", DataTypeIds.Double),
                    ("NewTargetProductTemp", DataTypeIds.Double),
                    ("NewTargetCO2Pressure", DataTypeIds.Double)
                });
            predefinedNodes.Add(changeProductMethod);

            // Adjust Fill Volume
            var adjustVolumeMethod = CreateMethodWithArgs(parent, "AdjustFillVolume", "Adjust Fill Volume", OnAdjustFillVolume,
                new[] { ("NewFillVolume", DataTypeIds.Double) });
            predefinedNodes.Add(adjustVolumeMethod);

            // Generate Lot Number
            var generateLotMethod = CreateMethod(parent, "GenerateLotNumber", "Generate Lot Number", OnGenerateLotNumber);
            predefinedNodes.Add(generateLotMethod);

            // Emergency Stop
            var emergencyStopMethod = CreateMethod(parent, "EmergencyStop", "Emergency Stop", OnEmergencyStop);
            predefinedNodes.Add(emergencyStopMethod);
        }

        private MethodState CreateMethod(FolderState parent, string name, string displayName, GenericMethodCalledEventHandler handler)
        {
            var method = new MethodState(parent)
            {
                NodeId = new NodeId(name, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = new LocalizedText("en", displayName),
                UserExecutable = true,
                Executable = true
            };
            method.OnCallMethod = handler;
            parent.AddChild(method);
            return method;
        }

        private MethodState CreateMethodWithArgs(FolderState parent, string name, string displayName, GenericMethodCalledEventHandler handler, (string, NodeId)[] inputArgs)
        {
            var method = CreateMethod(parent, name, displayName, handler);

            if (inputArgs?.Length > 0)
            {
                method.InputArguments = new PropertyState<Argument[]>(method)
                {
                    NodeId = new NodeId($"{name}_InputArguments", NamespaceIndex),
                    BrowseName = BrowseNames.InputArguments,
                    DisplayName = BrowseNames.InputArguments,
                    TypeDefinitionId = VariableTypeIds.PropertyType,
                    ReferenceTypeId = ReferenceTypeIds.HasProperty,
                    DataType = DataTypeIds.Argument,
                    ValueRank = ValueRanks.OneDimension,
                    Value = inputArgs.Select((arg, i) => new Argument
                    {
                        Name = arg.Item1,
                        Description = new LocalizedText(arg.Item1),
                        DataType = arg.Item2,
                        ValueRank = ValueRanks.Scalar
                    }).ToArray()
                };
                method.AddChild(method.InputArguments);
            }

            return method;
        }

        // Method handlers
        private ServiceResult OnStartMachine(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_machine.StartMachine();
            return ServiceResult.Good;
        }

        private ServiceResult OnStopMachine(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_machine.StopMachine();
            return ServiceResult.Good;
        }

        private ServiceResult OnLoadProductionOrder(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (inputArguments.Count == 9)
            {
                m_machine.LoadProductionOrder(
                    inputArguments[0]?.ToString() ?? "",
                    inputArguments[1]?.ToString() ?? "",
                    Convert.ToUInt32(inputArguments[2]),
                    Convert.ToDouble(inputArguments[3]),
                    Convert.ToDouble(inputArguments[4]),
                    Convert.ToDouble(inputArguments[5]),
                    Convert.ToDouble(inputArguments[6]),
                    Convert.ToDouble(inputArguments[7]),
                    Convert.ToDouble(inputArguments[8])
                );
            }
            return ServiceResult.Good;
        }

        private ServiceResult OnEnterMaintenanceMode(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_machine.EnterMaintenanceMode();
            return ServiceResult.Good;
        }

        private ServiceResult OnStartCIPCycle(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_machine.StartCIPCycle();
            return ServiceResult.Good;
        }

        private ServiceResult OnStartSIPCycle(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_machine.StartSIPCycle();
            return ServiceResult.Good;
        }

        private ServiceResult OnResetCounters(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_machine.ResetCounters();
            return ServiceResult.Good;
        }

        private ServiceResult OnChangeProduct(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (inputArguments.Count == 4)
            {
                m_machine.ChangeProduct(
                    inputArguments[0]?.ToString() ?? "",
                    Convert.ToDouble(inputArguments[1]),
                    Convert.ToDouble(inputArguments[2]),
                    Convert.ToDouble(inputArguments[3])
                );
            }
            return ServiceResult.Good;
        }

        private ServiceResult OnAdjustFillVolume(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            if (inputArguments.Count == 1)
            {
                m_machine.AdjustFillVolume(Convert.ToDouble(inputArguments[0]));
            }
            return ServiceResult.Good;
        }

        private ServiceResult OnGenerateLotNumber(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            var lotNumber = m_machine.GenerateLotNumber();
            if (outputArguments != null)
            {
                outputArguments.Add(lotNumber);
            }
            return ServiceResult.Good;
        }

        private ServiceResult OnEmergencyStop(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_machine.EmergencyStop();
            return ServiceResult.Good;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_simulationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}