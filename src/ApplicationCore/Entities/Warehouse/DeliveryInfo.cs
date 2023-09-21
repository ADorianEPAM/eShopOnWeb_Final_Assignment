﻿using System.Collections.Generic;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;

namespace Microsoft.eShopWeb.ApplicationCore.Entities.Warehouse;
public readonly record struct DeliveryInfo(string id,
                                           Address shippingAddress,
                                           IEnumerable<WarehouseOrderInfo> orderInfo,
                                           string finalPrice);
