// Chapter 28: event-driven notifications over a queue that delivers AT LEAST ONCE.
// 8 "task finished" events are published; the consumer emails the person waiting for each task.
//   dotnet run --project Ch28.Notify -- naive | idempotent
using Ch28.Notify;

string mode = args.FirstOrDefault() ?? "naive";
var log = (string s) => Console.WriteLine(s);
var queue = new AtLeastOnceQueue(lostAckEvery: 3);
var mail = new MailServer(failFirstFor: 104);
var handler = new NotificationHandler(mail, idempotent: mode == "idempotent", log);

for (int i = 1; i <= 8; i++)
    queue.Publish(new TaskFinished($"evt-{i}", TaskId: 100 + i, NotifyEmail: $"user{i}@example.com"));

await queue.ConsumeAllAsync(handler.HandleAsync, log);

int unique = mail.Sent.Distinct().Count();
Console.WriteLine($"\n{mode}: 8 events, {queue.Deliveries} deliveries, {mail.Sent.Count} emails sent " +
                  $"({mail.Sent.Count - unique} duplicates), {queue.DeadLetters.Count} dead letters.");
