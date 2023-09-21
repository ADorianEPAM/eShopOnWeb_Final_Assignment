namespace Microsoft.eShopWeb.ApplicationCore.Entities.Warehouse;

//Would be better to store these values in a key vault.
//Only for demo purposes!
public static class WarehouseConnections
{
    public const string ADD_ORDER_URL = "https://eshoponwebfinaldeliveryorderprocessor.azurewebsites.net/api/DeliveryOrderProcessor";
    public const string SERVICE_BUS_CONNECTION_STRING = "Endpoint=sb://eshoponwebfinal.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=LurXcwhup/fHrb2R/ArWhDmeAdp0hu6MF+ASbL6xos8=";
    public const string SERVICE_BUS_QUEUE = "warehouseorders";
}
