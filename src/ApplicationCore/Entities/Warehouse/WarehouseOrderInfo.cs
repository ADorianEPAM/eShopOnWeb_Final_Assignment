using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Entities.Warehouse;
public readonly record struct WarehouseOrderInfo(int Id, int Quantity);
