using Opc.Ua;
using Opc.Ua.Server;

namespace BeverageFillingLineServer
{
    public class BeverageFillingLineServer : StandardServer
    {
        protected override ServerProperties LoadServerProperties()
        {
            return new ServerProperties
            {
                ManufacturerName = "FluidFill Systems",
                ProductName = "FluidFill Express Simulator",
                ProductUri = "http://fluidfill.com/server/v1.0",
                SoftwareVersion = "1.0.0",
                BuildNumber = "1",
                BuildDate = DateTime.Now
            };
        }
    }
}
