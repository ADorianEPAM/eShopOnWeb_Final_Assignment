using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.Warehouse;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Newtonsoft.Json;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

        Guard.Against.Null(basket, nameof(basket));
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        #region ServiceBus communication
        //Would be a better way to store it as an enviroment variable.
        //await using var client = new ServiceBusClient(Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING"));
        await using var client = new ServiceBusClient(WarehouseConnections.SERVICE_BUS_CONNECTION_STRING);
        await using ServiceBusSender sender = client.CreateSender(WarehouseConnections.SERVICE_BUS_QUEUE);

        try
        {
            var warehouseData = basket.Items.Select(x => new WarehouseOrderInfo(x.CatalogItemId, x.Quantity));
            string messageBody = JsonConvert.SerializeObject(warehouseData);
            var message = new ServiceBusMessage(messageBody);
            await sender.SendMessageAsync(message);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
        }
        finally
        {
            await sender.DisposeAsync();
            await client.DisposeAsync();
        }
        #endregion

        #region Http Calls
        using var deliveryClient = new HttpClient();
        var deliveryWarehouseData = basket.Items.Select(x => new WarehouseOrderInfo(x.CatalogItemId, x.Quantity));
        var deliveryData = new DeliveryInfo(Guid.NewGuid().ToString(),
                                            shippingAddress,
                                            deliveryWarehouseData,
                                            items.Aggregate(0.0m,
                                                           (p, i) => p += i.UnitPrice * i.Units,
                                                           f => f.ToString()));
        var content = new StringContent(JsonConvert.SerializeObject(deliveryData), Encoding.UTF8, "application/json");
        var response = await deliveryClient.PostAsync(WarehouseConnections.ADD_ORDER_URL, content);
        #endregion
        await _orderRepository.AddAsync(order);
    }
}
