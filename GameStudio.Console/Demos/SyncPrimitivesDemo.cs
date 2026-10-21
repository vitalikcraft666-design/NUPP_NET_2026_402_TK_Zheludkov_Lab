using System.Collections.Concurrent;
using static GameStudio.ConsoleApp.ConsoleUi;

namespace GameStudio.ConsoleApp.Demos;

/// <summary>
/// ЛР2: приклади використання примітивів синхронізації на «ігрових» сценаріях.
/// </summary>
internal static class SyncPrimitivesDemo
{
    public static async Task RunAsync()
    {
        PrintSection("7. Примітиви синхронізації");

        DemonstrateRaceConditionLockInterlocked();
        DemonstrateMonitor();
        DemonstrateSemaphore();
        await DemonstrateSemaphoreSlimAsync();
        DemonstrateAutoResetEvent();
        DemonstrateManualResetEventAndCountdown();
        DemonstrateReaderWriterLock();
        DemonstrateBarrier();
        DemonstrateMutex();
    }

    // 7.1 Стан гонки, lock та Interlocked
    private static void DemonstrateRaceConditionLockInterlocked()
    {
        PrintSubsection("7.1 Стан гонки vs lock vs Interlocked (лічильник очок, 1 000 000 інкрементів з різних потоків)");
        const int iterations = 1_000_000;

        // без синхронізації: score++ — це «прочитати, додати, записати», і потоки перезаписують результати одне одного
        int unsafeScore = 0;
        Parallel.For(0, iterations, _ => unsafeScore++);

        // lock: у критичну секцію одночасно заходить лише один потік
        int lockedScore = 0;
        object scoreLock = new();
        Parallel.For(0, iterations, _ =>
        {
            lock (scoreLock)
            {
                lockedScore++;
            }
        });

        // Interlocked: атомарна операція процесора, без блокування
        int atomicScore = 0;
        Parallel.For(0, iterations, _ => Interlocked.Increment(ref atomicScore));

        Console.WriteLine($"    без синхронізації: {unsafeScore:N0} (втрачено оновлень: {iterations - unsafeScore:N0})");
        Console.WriteLine($"    lock:              {lockedScore:N0}");
        Console.WriteLine($"    Interlocked:       {atomicScore:N0}");
    }

    // 7.2 Monitor.TryEnter — спроба захопити блокування з тайм-аутом
    private static void DemonstrateMonitor()
    {
        PrintSubsection("7.2 Monitor.TryEnter — спроба зайняти слот збереження гри з тайм-аутом");
        object saveSlot = new();
        using var slotTaken = new ManualResetEventSlim(false);

        Task autosave = Task.Run(() =>
        {
            lock (saveSlot)
            {
                slotTaken.Set();
                Thread.Sleep(300); // автозбереження триває 300 мс
            }
        });

        slotTaken.Wait();
        bool quickTry = Monitor.TryEnter(saveSlot, TimeSpan.FromMilliseconds(50));
        if (quickTry)
        {
            Monitor.Exit(saveSlot);
        }

        Console.WriteLine($"    TryEnter(50 мс) під час автозбереження → {quickTry} (слот зайнятий, потік не завис)");

        bool patientTry = Monitor.TryEnter(saveSlot, TimeSpan.FromSeconds(2));
        try
        {
            Console.WriteLine($"    TryEnter(2 с)                         → {patientTry} (дочекались, поки автозбереження звільнить слот)");
        }
        finally
        {
            if (patientTry)
            {
                Monitor.Exit(saveSlot);
            }
        }

        autosave.Wait();
    }

    // 7.3 Semaphore — обмеження кількості потоків, що одночасно використовують ресурс
    private static void DemonstrateSemaphore()
    {
        PrintSubsection("7.3 Semaphore — рендер-ферма на 2 машини, 6 сцен у 6 окремих потоках (Thread)");
        using var renderFarm = new Semaphore(initialCount: 2, maximumCount: 2);
        var meter = new ConcurrencyMeter();
        var log = new ConcurrentQueue<string>();

        List<Thread> threads = Enumerable.Range(1, 6).Select(scene => new Thread(() =>
        {
            renderFarm.WaitOne(); // зайняти одну з двох машин (або чекати)
            try
            {
                int busy = meter.Enter();
                log.Enqueue($"    сцена {scene}: рендер почато у потоці {Environment.CurrentManagedThreadId}, зайнято машин: {busy}");
                Thread.Sleep(80);
                meter.Exit();
            }
            finally
            {
                renderFarm.Release(); // звільнити машину
            }
        })).ToList();

        threads.ForEach(thread => thread.Start());
        threads.ForEach(thread => thread.Join());

        foreach (string line in log)
        {
            Console.WriteLine(line);
        }

        Console.WriteLine($"    максимум одночасно працювало: {meter.Max} з 6 потоків");
    }

    // 7.4 SemaphoreSlim — асинхронне очікування WaitAsync (цей самий механізм захищає файл у CrudServiceAsync)
    private static async Task DemonstrateSemaphoreSlimAsync()
    {
        PrintSubsection("7.4 SemaphoreSlim + WaitAsync — 8 асинхронних завантажень ресурсів, не більше 3 одночасно");
        using var downloadSlots = new SemaphoreSlim(3, 3);
        var meter = new ConcurrencyMeter();

        IEnumerable<Task> downloads = Enumerable.Range(1, 8).Select(async _ =>
        {
            await downloadSlots.WaitAsync(); // очікування без блокування потоку
            try
            {
                meter.Enter();
                await Task.Delay(60); // імітація завантаження
                meter.Exit();
            }
            finally
            {
                downloadSlots.Release();
            }
        });

        await Task.WhenAll(downloads);
        Console.WriteLine($"    завантажено 8 ресурсів; максимум одночасних завантажень: {meter.Max}");
    }

    // 7.5 AutoResetEvent — сигнал, що пропускає рівно один потік і автоматично скидається
    private static void DemonstrateAutoResetEvent()
    {
        PrintSubsection("7.5 AutoResetEvent — сервер збірок обробляє запити по одному");
        string[] platforms = { "Windows", "Android", "WebGL" };
        using var buildRequested = new AutoResetEvent(false);
        using var buildFinished = new AutoResetEvent(false);
        string currentPlatform = string.Empty;

        var buildServer = new Thread(() =>
        {
            for (int i = 0; i < platforms.Length; i++)
            {
                buildRequested.WaitOne(); // чекаємо сигналу; після пробудження подія сама скидається
                Console.WriteLine($"    сервер збірок: отримав сигнал → збираю {currentPlatform}");
                buildFinished.Set();
            }
        });
        buildServer.Start();

        foreach (string platform in platforms)
        {
            currentPlatform = platform;
            Console.WriteLine($"    головний потік: Set() — запит на збірку {platform}");
            buildRequested.Set();
            buildFinished.WaitOne();
        }

        buildServer.Join();

        // один Set() пропускає лише ОДИН з потоків, що чекають
        using var ticket = new AutoResetEvent(false);
        int passed = 0;
        List<Thread> waiters = Enumerable.Range(0, 3).Select(_ => new Thread(() =>
        {
            ticket.WaitOne();
            Interlocked.Increment(ref passed);
        })).ToList();
        waiters.ForEach(thread => thread.Start());

        Thread.Sleep(100);
        ticket.Set();
        Thread.Sleep(100);
        int afterOneSet = Volatile.Read(ref passed);

        // решту потоків відпускаємо наступними сигналами — кожен Set() пропускає ще одного
        int extraSignals = 0;
        while (Volatile.Read(ref passed) < waiters.Count)
        {
            ticket.Set();
            extraSignals++;
            Thread.Sleep(30);
        }

        waiters.ForEach(thread => thread.Join());
        Console.WriteLine($"    3 потоки чекають; після одного Set() пройшов {afterOneSet}; " +
                          $"решта — після ще {extraSignals} Set() (усього пройшло {passed})");
    }

    // 7.6 ManualResetEventSlim + CountdownEvent
    private static void DemonstrateManualResetEventAndCountdown()
    {
        PrintSubsection("7.6 ManualResetEventSlim + CountdownEvent — старт матчу для 4 гравців");
        using var matchStart = new ManualResetEventSlim(false);
        using var playersReady = new CountdownEvent(4);
        int started = 0;

        for (int i = 0; i < 4; i++)
        {
            new Thread(() =>
            {
                matchStart.Wait(); // усі гравці чекають на один спільний сигнал
                Interlocked.Increment(ref started);
                playersReady.Signal(); // повідомляємо, що гравець стартував
            }).Start();
        }

        Thread.Sleep(100);
        Console.WriteLine($"    до Set(): стартувало гравців — {Volatile.Read(ref started)}");
        matchStart.Set(); // ManualResetEvent відпускає ВСІХ, хто чекає, і лишається у стані «сигнал»
        playersReady.Wait(); // CountdownEvent: чекаємо, поки лічильник дійде до нуля
        Console.WriteLine($"    після одного Set(): стартували всі {started}; IsSet = {matchStart.IsSet}; " +
                          $"CountdownEvent.CurrentCount = {playersReady.CurrentCount}");
    }

    // 7.7 ReaderWriterLockSlim — багато читачів одночасно, письменник — ексклюзивно
    private static void DemonstrateReaderWriterLock()
    {
        PrintSubsection("7.7 ReaderWriterLockSlim — таблиця рекордів: 36 читань і 4 записи паралельно");
        using var tableLock = new ReaderWriterLockSlim();
        var leaderboard = new List<int> { 100 };
        var readers = new ConcurrencyMeter();
        var writers = new ConcurrencyMeter();
        int reads = 0, writes = 0;

        Parallel.For(0, 40, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            if (i % 10 == 0)
            {
                tableLock.EnterWriteLock();
                try
                {
                    writers.Enter();
                    leaderboard.Add(Random.Shared.Next(100, 10_000));
                    Thread.Sleep(10);
                    writers.Exit();
                    Interlocked.Increment(ref writes);
                }
                finally
                {
                    tableLock.ExitWriteLock();
                }
            }
            else
            {
                tableLock.EnterReadLock();
                try
                {
                    readers.Enter();
                    _ = leaderboard.Max();
                    Thread.Sleep(10);
                    readers.Exit();
                    Interlocked.Increment(ref reads);
                }
                finally
                {
                    tableLock.ExitReadLock();
                }
            }
        });

        Console.WriteLine($"    читань: {reads}, записів: {writes}; одночасних читачів (макс.): {readers.Max}; " +
                          $"одночасних письменників (макс.): {writers.Max}; рекорд: {leaderboard.Max()}");
    }

    // 7.8 Barrier — потоки чекають один одного в кінці кожної фази
    private static void DemonstrateBarrier()
    {
        PrintSubsection("7.8 Barrier — фізика, ШІ та рендер синхронізуються в кінці кожного кадру");
        using var frameBarrier = new Barrier(3, barrier =>
            Console.WriteLine($"    кадр {barrier.CurrentPhaseNumber + 1}: усі 3 підсистеми завершили роботу → наступний кадр"));

        List<Thread> systems = new[] { "фізика", "ШІ", "рендер" }.Select(_ => new Thread(() =>
        {
            for (int frame = 0; frame < 3; frame++)
            {
                Thread.Sleep(Random.Shared.Next(5, 30)); // кожна підсистема працює різний час
                frameBarrier.SignalAndWait();
            }
        })).ToList();

        systems.ForEach(thread => thread.Start());
        systems.ForEach(thread => thread.Join());
    }

    // 7.9 Mutex — іменований м'ютекс видно всім процесам (наприклад, щоб гра не запускалась двічі)
    private static void DemonstrateMutex()
    {
        PrintSubsection("7.9 Mutex — перевірка, що запущено лише один екземпляр програми");
        using var mutex = new Mutex(initiallyOwned: false, name: "GameStudio.Lab2.SingleInstance", out bool createdNew);
        bool acquired = mutex.WaitOne(TimeSpan.Zero);
        try
        {
            Console.WriteLine($"    м'ютекс створено цим процесом: {createdNew}; захоплено: {acquired} → запущено єдиний екземпляр");
        }
        finally
        {
            if (acquired)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    // допоміжний клас — рахує, скільки потоків одночасно перебувають у ділянці коду, і запам'ятовує максимум
    private sealed class ConcurrencyMeter
    {
        private int _current;
        private int _max;

        public int Max => Volatile.Read(ref _max);

        public int Enter()
        {
            int now = Interlocked.Increment(ref _current);
            int seen;
            while (now > (seen = Volatile.Read(ref _max)) && Interlocked.CompareExchange(ref _max, now, seen) != seen)
            {
            }

            return now;
        }

        public void Exit() => Interlocked.Decrement(ref _current);
    }
}
