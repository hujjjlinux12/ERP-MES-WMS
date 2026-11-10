using Manufacturing.Erp.Domain;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static void Reject(Action action, int status)
{
    try { action(); }
    catch (BusinessException exception) when (exception.Status == status) { return; }
    throw new Exception($"Expected business rejection {status}.");
}

var store = new ErpStore();
var order = store.Create(new("SUP-001", [new("PCB-001", 10, 2.5m), new("PANEL-001", 5, 100)]));
Check(order.Total == 525, "Order total.");
var first = order.Lines[0].Id;
var second = order.Lines[1].Id;
ReceiveRequest Request(params ReceiptInputLine[] lines) => new(Guid.NewGuid(), "RM-01", "warehouse-demo", lines);
Reject(() => store.Approve(order.Id, "approver-demo"), 409);
Reject(() => store.Receive(order.Id, Request(new ReceiptInputLine(first, 1))), 409);
store.Submit(order.Id, "buyer-demo");
store.Approve(order.Id, "approver-demo");
var input = Request(new ReceiptInputLine(first, 4));
var receipt = store.Receive(order.Id, input);
Check(store.GetOrder(order.Id).Status == OrderStatus.PartiallyReceived, "Partial status.");
Check(store.Receive(order.Id, input).Id == receipt.Id, "Retry should return original receipt.");
Check(store.GetLedger().Length == 1, "Retry must not duplicate ledger.");
Reject(() => store.Receive(order.Id, input with { Lines = [new(first, 3)] }), 409);
Reject(() => store.Receive(order.Id, Request(new ReceiptInputLine(first, 2), new ReceiptInputLine(second, 6))), 400);
Check(store.GetLedger().Length == 1 && store.GetOrder(order.Id).Lines[0].ReceivedQuantity == 4,
    "Failed multiline receipt must not change any state.");
store.Receive(order.Id, Request(new ReceiptInputLine(first, 6), new ReceiptInputLine(second, 5)));
Check(store.GetOrder(order.Id).Status == OrderStatus.Received, "Fully received status.");
Check(store.GetBalances().Single(x => x.MaterialCode == "PCB-001").Quantity == 10, "PCB balance.");
Check(store.GetBalances().Single(x => x.MaterialCode == "PANEL-001").Quantity == 5, "Panel balance.");
Reject(() => store.Receive(order.Id, Request(new ReceiptInputLine(first, 1))), 409);
Check(store.Receive(order.Id, input).Id == receipt.Id, "Retry after order completion.");
foreach (var balance in store.GetBalances())
    Check(balance.Quantity == store.GetLedger().Where(x => x.MaterialCode == balance.MaterialCode &&
        x.WarehouseCode == balance.WarehouseCode).Sum(x => x.Quantity), "Ledger reconciliation.");

// Concurrent duplicate requests must create only one receipt and one stock movement.
var concurrent = store.Create(new("SUP-001", [new("PCB-001", 1, 1)]));
store.Submit(concurrent.Id, "buyer-demo");
store.Approve(concurrent.Id, "approver-demo");
var duplicate = Request(new ReceiptInputLine(concurrent.Lines[0].Id, 1));
var outputs = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => store.Receive(concurrent.Id, duplicate))));
Check(outputs.Select(x => x.Id).Distinct().Count() == 1, "Concurrent idempotency.");
Check(store.GetLedger().Count(x => x.OrderId == concurrent.Id) == 1, "Concurrent duplicate stock movement.");
Console.WriteLine("All domain checks passed.");
