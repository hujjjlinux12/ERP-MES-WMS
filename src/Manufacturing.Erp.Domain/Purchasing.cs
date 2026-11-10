namespace Manufacturing.Erp.Domain;

public sealed class BusinessException(string message, int status = 400) : Exception(message)
{
    public int Status { get; } = status;
}

public enum OrderStatus { Draft, Submitted, Approved, PartiallyReceived, Received }
public sealed record NamedItem(string Code, string Name);
public sealed record Material(string Code, string Name, string Unit);
public sealed record NewLine(string MaterialCode, decimal Quantity, decimal UnitPrice);
public sealed record NewOrder(string SupplierCode, NewLine[] Lines);
public sealed record ReceiptInputLine(Guid OrderLineId, decimal Quantity);
public sealed record ReceiveRequest(Guid RequestId, string WarehouseCode, string Actor, ReceiptInputLine[] Lines);
public sealed record OrderLine(Guid Id, string MaterialCode, decimal Quantity, decimal UnitPrice, decimal ReceivedQuantity);
public sealed record Order(Guid Id, string Number, string SupplierCode, OrderStatus Status,
    DateTimeOffset CreatedAt, OrderLine[] Lines, string? SubmittedBy = null,
    DateTimeOffset? SubmittedAt = null, string? ApprovedBy = null, DateTimeOffset? ApprovedAt = null)
{
    public decimal Total => decimal.Round(Lines.Sum(x => x.Quantity * x.UnitPrice), 2, MidpointRounding.AwayFromZero);
}
public sealed record Receipt(Guid Id, Guid RequestId, Guid OrderId, string WarehouseCode,
    string Actor, DateTimeOffset ReceivedAt, ReceiptInputLine[] Lines);
public sealed record Balance(string MaterialCode, string WarehouseCode, decimal Quantity);
public sealed record LedgerEntry(Guid Id, Guid ReceiptId, Guid OrderId, Guid OrderLineId,
    string MaterialCode, string WarehouseCode, decimal Quantity, DateTimeOffset Time, string Actor);

// Single-process teaching store. Persistence and multi-instance concurrency are separate increments.
public sealed class ErpStore
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, Order> orders = [];
    private readonly Dictionary<Guid, Receipt> receipts = [];
    private readonly Dictionary<(string Material, string Warehouse), decimal> balances = [];
    private readonly List<LedgerEntry> ledger = [];
    private readonly Material[] materials = [new("PCB-001", "显示器主板 PCB", "PCS"), new("PANEL-001", "显示面板", "PCS")];
    private readonly NamedItem[] suppliers = [new("SUP-001", "模拟电子供应商")];
    private readonly NamedItem[] warehouses = [new("RM-01", "原料仓")];

    public Material[] Materials => materials.ToArray();
    public NamedItem[] Suppliers => suppliers.ToArray();
    public NamedItem[] Warehouses => warehouses.ToArray();
    private static Order Copy(Order order) => order with { Lines = order.Lines.ToArray() };
    private static Receipt Copy(Receipt receipt) => receipt with { Lines = receipt.Lines.ToArray() };
    private Order Find(Guid id) => orders.TryGetValue(id, out var order) ? order : throw new BusinessException("采购订单不存在。", 404);
    private static void Require(bool condition, string message, int status = 400)
    {
        if (!condition) throw new BusinessException(message, status);
    }
    private static void CheckActor(string actor) => Require(!string.IsNullOrWhiteSpace(actor) && actor.Length <= 100, "操作者必填且不超过 100 字符。");
    private static bool Precision(decimal value, int digits) => decimal.Round(value, digits) == value;

    public Order[] ListOrders() { lock (gate) return orders.Values.Select(Copy).ToArray(); }
    public Order GetOrder(Guid id) { lock (gate) return Copy(Find(id)); }
    public Balance[] GetBalances()
    {
        lock (gate) return balances.Select(x => new Balance(x.Key.Material, x.Key.Warehouse, x.Value)).ToArray();
    }
    public LedgerEntry[] GetLedger() { lock (gate) return ledger.ToArray(); }

    public Order Create(NewOrder input)
    {
        lock (gate)
        {
            Require(suppliers.Any(x => x.Code == input.SupplierCode), "供应商不存在。");
            Require(input.Lines is { Length: > 0 and <= 100 }, "订单必须包含 1–100 行。");
            Require(input.Lines.All(x => x is not null), "订单行不能为空。");
            Require(input.Lines.Select(x => x.MaterialCode).Distinct().Count() == input.Lines.Length, "订单内料号不能重复。");
            foreach (var line in input.Lines)
            {
                Require(materials.Any(x => x.Code == line.MaterialCode), "物料不存在。");
                Require(line.Quantity > 0 && line.Quantity <= 1_000_000 && Precision(line.Quantity, 3), "数量须大于 0、不超过 1000000，最多三位小数。");
                Require(line.UnitPrice >= 0 && line.UnitPrice <= 1_000_000 && Precision(line.UnitPrice, 4), "单价须在 0–1000000 之间，最多四位小数。");
            }
            var id = Guid.NewGuid();
            var order = new Order(id, $"PO-{id:N}", input.SupplierCode, OrderStatus.Draft, DateTimeOffset.UtcNow,
                input.Lines.Select(x => new OrderLine(Guid.NewGuid(), x.MaterialCode, x.Quantity, x.UnitPrice, 0)).ToArray());
            orders.Add(id, order);
            return Copy(order);
        }
    }

    public Order Submit(Guid id, string actor)
    {
        lock (gate)
        {
            CheckActor(actor);
            var order = Find(id);
            Require(order.Status == OrderStatus.Draft, "只有草稿可以提交。", 409);
            order = order with { Status = OrderStatus.Submitted, SubmittedBy = actor, SubmittedAt = DateTimeOffset.UtcNow };
            orders[id] = order;
            return Copy(order);
        }
    }

    public Order Approve(Guid id, string actor)
    {
        lock (gate)
        {
            CheckActor(actor);
            var order = Find(id);
            Require(order.Status == OrderStatus.Submitted, "只有已提交订单可以审批。", 409);
            order = order with { Status = OrderStatus.Approved, ApprovedBy = actor, ApprovedAt = DateTimeOffset.UtcNow };
            orders[id] = order;
            return Copy(order);
        }
    }

    public Receipt Receive(Guid id, ReceiveRequest input)
    {
        lock (gate)
        {
            CheckActor(input.Actor);
            Require(input.RequestId != Guid.Empty, "requestId 不能为空。");
            Require(warehouses.Any(x => x.Code == input.WarehouseCode), "仓库不存在。");
            Require(input.Lines is { Length: > 0 and <= 100 }, "收货必须包含 1–100 行。");
            Require(input.Lines.All(x => x is not null), "收货行不能为空。");
            Require(input.Lines.Select(x => x.OrderLineId).Distinct().Count() == input.Lines.Length, "收货行不能重复。");
            var canonical = input.Lines.OrderBy(x => x.OrderLineId).ToArray();
            if (receipts.TryGetValue(input.RequestId, out var previous))
            {
                Require(previous.OrderId == id && previous.WarehouseCode == input.WarehouseCode &&
                    previous.Actor == input.Actor && previous.Lines.SequenceEqual(canonical), "requestId 已用于不同收货内容。", 409);
                return Copy(previous);
            }
            var order = Find(id);
            Require(order.Status is OrderStatus.Approved or OrderStatus.PartiallyReceived, "当前订单状态不允许收货。", 409);
            foreach (var line in canonical)
            {
                var source = order.Lines.SingleOrDefault(x => x.Id == line.OrderLineId);
                Require(source is not null, "收货行不属于当前订单。");
                Require(line.Quantity > 0 && Precision(line.Quantity, 3), "收货数量须大于 0，最多三位小数。");
                Require(line.Quantity <= source!.Quantity - source.ReceivedQuantity, "累计收货数量不能超过订单数量。");
            }
            // Validate all lines before changing any state.
            var receipt = new Receipt(Guid.NewGuid(), input.RequestId, id, input.WarehouseCode,
                input.Actor, DateTimeOffset.UtcNow, canonical);
            var updated = order.Lines.Select(line => line with {
                ReceivedQuantity = line.ReceivedQuantity + (canonical.SingleOrDefault(x => x.OrderLineId == line.Id)?.Quantity ?? 0)
            }).ToArray();
            foreach (var line in canonical)
            {
                var source = order.Lines.Single(x => x.Id == line.OrderLineId);
                var key = (source.MaterialCode, input.WarehouseCode);
                balances[key] = balances.GetValueOrDefault(key) + line.Quantity;
                ledger.Add(new LedgerEntry(Guid.NewGuid(), receipt.Id, id, source.Id, source.MaterialCode,
                    input.WarehouseCode, line.Quantity, receipt.ReceivedAt, input.Actor));
            }
            orders[id] = order with { Lines = updated, Status = updated.All(x => x.ReceivedQuantity == x.Quantity)
                ? OrderStatus.Received : OrderStatus.PartiallyReceived };
            receipts.Add(input.RequestId, receipt);
            return Copy(receipt);
        }
    }
}
