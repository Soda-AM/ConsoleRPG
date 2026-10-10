using System.Text.Json;
using static ConsoleRPG.GameSessionManager;

namespace ConsoleRPG
{
    internal class Program
    {
        static void Main(string[] args)
        {

            GameUI.ShowMainMenu();
            
        }
    }

    class Player
    {
        public int Damage { get; set; }
        public int Health { get; set; }
        public int Xp {  get; set; }
        public int Level { get; set; } 
        public int XpForNextLevel { get; set; }

        public Player()
        {
            try
            {
                string jsonPlayerConfig = File.ReadAllText("PlayerConfig.json");
                PlayerConfig playerConfig = JsonSerializer.Deserialize<PlayerConfig>(jsonPlayerConfig) ?? new PlayerConfig();

                Health = playerConfig.Health; 
                Damage = playerConfig.Damage;;
                XpForNextLevel = playerConfig.XpForNextLevel;
            }
            catch (FileNotFoundException)
            {
                Console.Clear();
                GameUI.WriteText("Не удалось найти нужный файл с настройками игрока", ConsoleColor.DarkGray, true);
                Console.WriteLine();
                Console.WriteLine();
            }
            catch (JsonException)
            {
                Console.Clear();
                GameUI.WriteText("Файл PlayerConfig.json поврежден", ConsoleColor.DarkGray, true);
                Console.WriteLine();
                Console.WriteLine();
            }
        }
        
        public void Attack(Enemy enemy)
        {
            Console.Write($"Вы нанесли ");
            GameUI.WriteText($"{Damage}", ConsoleColor.Red);
            Console.WriteLine($" урона {enemy.Name}");
            enemy.TakeDamage(Damage);

            if (enemy.Health > 0)
            {
                Console.Write($"У {enemy.Name} осталось ");
                GameUI.WriteText($"{enemy.Health}", ConsoleColor.Green);    
                Console.WriteLine(" хп\n");
            }
        }

        public void GetXp(Enemy enemy)
        {
            Xp += enemy.Xp;

            if (Xp >= XpForNextLevel)
            {
                LevelUp();
                XpForNextLevel += 50;
                Xp = 0;
                Console.Write($"Уровень повышен\nТекущий уровень: ");
                GameUI.WriteText($"{Level}\n", ConsoleColor.Green);
            }
            else
            {
                Console.Write("Полученно ");
                GameUI.WriteText($"{enemy.Xp} ", ConsoleColor.Green);
                Console.WriteLine("опыта");
                Console.Write("До следующего уровеня: ");
                GameUI.WriteText($"{XpForNextLevel - Xp}\n\n", ConsoleColor.Green);
            }
        }

        private void LevelUp()
        {
            Level++;
            ManagerStatistics.statistics.MaxLevel = Level;
            Health += 10;
            Damage += 5;
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;

            if (Health <= 0)
            {
                PlayerDead();
            }
        }

        private void PlayerDead()
        {
            GameUI.ShowDeadMenu();
        }
    }

    abstract class Enemy
    {
        private int health;
        private int xp;
        private string? name;
        private bool isDead;

        public delegate void DiedEnemy( Enemy enemy );
        public event DiedEnemy? Died;
        
        public int Health
        {
            get { return health; }
            set {
                if (value > 0)
                {
                    health = value;
                }
                else
                {
                    health = 0;
                    EnemyDead();
                }
            }
        }

        public int Xp 
        {
            get { return xp; } 
            set
            {
                if (value >= 0 && value <= 100)
                    xp = value;
                else
                    xp = 0;
            }
        }

        public string? Name
        {
            get{ return name; }
            set
            {
                if (value == null)
                    name = "Ошибка: Название неизвестно";
                else
                    name = value;
            }
        }
        public int Damage { get; set; }


        public Enemy(int health, int damage, int xp, string? name)
        {
            Health = health;
            Damage = damage;
            Xp = xp;
            Name = name;
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
        }

        public virtual void MakeDamage(Player player)
        {
            player.TakeDamage(Damage);
            GameUI.WriteText($"У вас осталось {player.Health} хп", ConsoleColor.Red);          
            Console.WriteLine();
        }

        protected void EnemyDead()
        {
            if (isDead)
                return;

            Console.WriteLine("Враг умер");

            isDead = true;
            Died?.Invoke(this);
           
        }

    }

    class ManagerWaves
    {
        public int Wave { get; set; }
        int Difficulty { get; set; } = 1;
        public List<Enemy> Enemies { get; private set; } = new();
        
        private Player player;

        public ManagerWaves(Player player)
        {
            this.player = player;
        }

        public void CreateWave(WaveEnemy[] wave)
        {

            foreach (WaveEnemy enemyInfo in wave)
            {
                for (int i = 0; i < enemyInfo.Count; i++)
                {
                    try
                    {
                        Enemy enemy = (Enemy)Activator.CreateInstance(enemyInfo.EnemyType)!;
                        enemy.Died += player.GetXp;
                        enemy.Died += DeleteEnemy;
                        enemy.Died += ManagerStatistics.statistics.IncreaseEnemiesKilled;
                        Enemies.Add(enemy);
                    }
                    catch (InvalidCastException)
                    {
                        GameUI.WriteText("Не удалось создать врага. Указан неверный тип врага", ConsoleColor.DarkGray, true);
                        Console.WriteLine();
                    }
                }
            }
        }

        public void DeleteEnemy(Enemy enemy)
        {
            Enemies.Remove(enemy);

            if (Enemies.Count == 0)
            {
                WaveEnded();
            }
        }

        private void WaveEnded()
        {
            Wave++;
            Difficulty++;
            ManagerStatistics.statistics.MaxWave = Wave;

            GameUI.ShowWaveEndedMenu(this);
        }

        public void StartNewWave()
        {
            Type[] enemyType = new Type[]
            {
                typeof(Zombie),
                typeof(Skeleton),
                typeof(Goblin),
            };

            Random random = new Random();
            WaveEnemy[] waveEnemies = {};
            double change = 0.2;

            if (random.NextDouble() < change)
            {
                waveEnemies = new WaveEnemy[]
                {
                    new WaveEnemy(enemyType[random.Next(0,3)], random.Next((int)(Difficulty * 0.5), (int)(Difficulty * 1.5))),
                    new WaveEnemy(enemyType[random.Next(0,3)], random.Next((int)(Difficulty * 0.5), (int)(Difficulty * 1.5))),
                    new WaveEnemy(enemyType[random.Next(0,3)], random.Next((int)(Difficulty * 0.5), (int)(Difficulty * 1.5))),
                };
            }
            else
            {
                waveEnemies = new WaveEnemy[]
                {
                    new WaveEnemy(enemyType[random.Next(0,3)], random.Next((Difficulty * 1), (int)(Difficulty * 1.5))),
                    new WaveEnemy(enemyType[random.Next(0,3)], random.Next((Difficulty * 1), (int)(Difficulty * 1.5))),
                };
            }

            CreateWave(waveEnemies);
        }
    }   

    class WaveEnemy
    {
        public Type EnemyType { get; set; }
        private int count;
        private int maxCount = 4;
        public int Count
        {
            get { return count; }
            set
            {
                if (value >= maxCount)
                {
                    count = maxCount;
                }
                else
                {
                    count = value;
                }
            }
        }

        public WaveEnemy(Type enemyType, int count)
        {
            EnemyType = enemyType;
            Count = count;
        }
    }

    #region Шаблоны врагов
    class Zombie : Enemy
    {
        public Zombie() : base(80, 10, 20, "Зомби")
        {
            
        }

        public override void MakeDamage(Player player)
        {
            Console.WriteLine($"{Name} ударил");
            base.MakeDamage(player);
        }
    }

    class Skeleton : Enemy
    {
        public Skeleton() : base(100, 5, 25, "Скелет")
        {

        }

        public override void MakeDamage(Player player)
        {
            Console.WriteLine($"{Name} выстрелил");

            double change = 0.2;

            if (Random.Shared.NextDouble() < change)
            {
                GameUI.WriteText("Вы увернулись от стрелы", ConsoleColor.DarkGray);
                Console.WriteLine();
                return;
            }
            else
            {
                base.MakeDamage(player);    
            }
        }
    }

    class Goblin : Enemy
    {
        private int criticalPercent;
        private int criticalDamage;
        private int CriticalPercent
        {
            get { return criticalPercent; }
            set
            {
                if (value > 0 && value < 100)
                    criticalPercent = value;
            }
        }
        private int CriticalDamage
        {
            get
            {
                criticalDamage = Damage + (Damage * CriticalPercent / 100);
                return criticalDamage;
            }
        }

        public Goblin() : base(60, 5, 10, "Гоблин")
        {
            CriticalPercent = 50;
        }

        public override void MakeDamage(Player player)
        {
            Console.WriteLine($"{Name} пырнул");

            double change = 0.2;

            if (Random.Shared.NextDouble() < change)
            {
                player.TakeDamage(CriticalDamage);
                GameUI.WriteText($"У вас осталось {player.Health} хп - Крит урон!", ConsoleColor.Red);
                Console.WriteLine();
                return;
            }
            else
            {
                base .MakeDamage(player);
            }
        }
    }
    #endregion

    // Классы для управления игрой
    class GameUI 
    {
        public static void WriteText(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        public static void WriteText(string text, bool center)
        {
            int x = (Console.WindowWidth - text.Length) / 2;
            Console.SetCursorPosition(x, Console.CursorTop);

            Console.Write(text);
        }

        public static void WriteText(string text, ConsoleColor color, bool center)
        {
            int x = (Console.WindowWidth - text.Length) / 2;
            Console.SetCursorPosition(x, Console.CursorTop);

            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        public static void ShowMenu()
        {
            WriteText("Добро пожаловать!", true);
            Console.WriteLine();

            WriteText("Выберите одно из следующих действий", true);
            Console.WriteLine();

            string text1 = "Начать новую игру";
            string text2 = "Посмотреть статистику";
            string text3 = "Выйти";

            string fullText =
                $"1. - {text1}    " +
                $"2. - {text2}    " +
                $"3. - {text3}";

            int x = (Console.WindowWidth - fullText.Length) / 2;
            Console.SetCursorPosition(x, Console.CursorTop);

            WriteText("1. - ", ConsoleColor.Red);
            Console.Write(text1 + "    ");

            WriteText("2. - ", ConsoleColor.Red);
            Console.Write(text2 + "    ");

            WriteText("3. - ", ConsoleColor.Red);
            Console.WriteLine(text3);
            Console.WriteLine();
        }

        public static void ShowMainMenu()
        {
            ShowMenu();

            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;

                if (key == (ConsoleKey)Actions.NewGame)
                {
                    GameManager.GameSessionManager.StartGame();
                    return;
                }
                else if (key == (ConsoleKey)Actions.Close)
                {
                    Environment.Exit(0);
                }
                else if (key == (ConsoleKey)Actions.ShowStat)
                {
                    ShowStatisticsMenu(ManagerStatistics.GlobalStatistic);
                    return;
                }
            }
        }

        public static void ShowStatisticsMenu(SampleStatistics statistics)
        {
            Console.Clear();

            WriteText("Статистика за все время", ConsoleColor.Yellow, true);
            Console.WriteLine();
            Console.WriteLine();

            ManagerStatistics.ReadStatistics();
            WriteText($"Максимальная волна: {statistics.MaxWave}", true);
            Console.WriteLine();
            WriteText($"Максимальный уровень игрока: {statistics.MaxLevel}", true);
            Console.WriteLine();
            WriteText($"Всего убито врагов: {statistics.EnemiesKilled}", true);
            Console.WriteLine();
            Console.WriteLine(  );

            string text1 = "Выйти в меню";
            string fullText = $"1. - {text1}    ";

            int x = (Console.WindowWidth - fullText.Length) / 2;
            Console.SetCursorPosition(x, Console.CursorTop);

            WriteText("1. - ", ConsoleColor.Red);
            Console.Write(text1 + "    ");
            Console.WriteLine();

            SampleAction statisticAction = new SampleAction()
            {               
                ConsoleKey = ConsoleKey.D1,
                Action = () =>
                {
                    Console.Clear();
                    ShowMainMenu();
                }     
            };
            CheckActionManager.CheckAction(statisticAction);
        }

        public static void ShowWaveEndedMenu(ManagerWaves managerWaves)
        {
            string text1 = "Начать новую волну";
            string text2 = "Выйти в меню";

            string fullText =
               $"1. - {text1}    " +
               $"2. - {text2}    ";

            WriteText($"Волна {managerWaves.Wave} успешно отраженна!", ConsoleColor.DarkGreen, true);
            Console.WriteLine();

            int x = (Console.WindowWidth - fullText.Length) / 2;
            Console.SetCursorPosition(x, Console.CursorTop);

            WriteText("1. - ", ConsoleColor.Red);
            Console.Write(text1 + "    ");

            WriteText("2. - ", ConsoleColor.Red);
            Console.Write(text2 + "    ");
            Console.WriteLine();

            SampleAction[] waveEndedAction = new SampleAction[]
            {
                new SampleAction()
                {
                    ConsoleKey = ConsoleKey.D1,
                    Action = () =>
                    {
                        WriteText("Началась новая волна", ConsoleColor.DarkRed, true);
                        Console.WriteLine();
                        managerWaves.StartNewWave();
                    }
                },

                new SampleAction()
                {
                    ConsoleKey = ConsoleKey.D2,
                    Action = () =>
                    {
                        GameManager.FinishedGame();
                        Console.Clear();
                        ShowMainMenu();
                    }
                }
            };
            CheckActionManager.CheckAction(waveEndedAction);
        }

        public static void ShowDeadMenu()
        {
            WriteText("Вы погибли", ConsoleColor.DarkRed, true);
            Console.WriteLine();
            Console.WriteLine();
            WriteText("Нажмите любую клавишу чтобы вернуться в меню", ConsoleColor.DarkGray, true);

            SampleAction deadAction = new SampleAction()
            {
                AnyKey = true,
                Action = () =>
                {
                    GameManager.FinishedGame();
                    Console.Clear();
                    ShowMainMenu();
                }
            };
            CheckActionManager.CheckAction(deadAction); 
        }
    }
    class GameManager
    {
        public static GameSessionManager GameSessionManager { get; private set; } = new GameSessionManager();
        
        public static void FinishedGame()
        {
            ManagerStatistics.SaveStatistics();
            GameSessionManager = new GameSessionManager();
        }
    }
    class GameSessionManager
    {
        public enum Actions
        {
            NewGame = ConsoleKey.D1,
            ShowStat = ConsoleKey.D2,
            Close = ConsoleKey.D3
        }

        private Player player;
        private ManagerWaves managerWaves; 
        private ManagerMove managerMove;

        public GameSessionManager()
        {
            player = new Player();
            managerWaves = new ManagerWaves(player);
            managerMove = new ManagerMove(player, managerWaves);
        }

        public void StartGame()
        {
            GameUI.WriteText("Началась новая игра", ConsoleColor.Green, true);
            Console.WriteLine();

            ManagerStatistics.ReadStatistics();
            managerWaves.StartNewWave();

            while (managerWaves.Enemies.Count > 0)
            {
                ConsoleKey key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.Spacebar && managerMove.IsPlayerMove)
                {
                    managerMove.PlayerMove();
                }
                else if (managerMove.IsEnemyMove)
                {
                    managerMove.EnemyMove();
                }
            }
        }
    }
    class ManagerMove
    {
        public bool IsPlayerMove { get; set; } = true;
        public bool IsEnemyMove { get; set; } = false;

        private Player player;
        private ManagerWaves managerWaves;

        public ManagerMove (Player player, ManagerWaves managerWaves)
        {
            this.player = player;
            this.managerWaves = managerWaves;
        }

        public void PlayerMove()
        {
            GameUI.WriteText("Ваш ход", ConsoleColor.DarkGreen, true);
            Console.WriteLine();

            IsPlayerMove = false;
            IsEnemyMove = true;

            foreach (var enemy in managerWaves.Enemies.ToList())
            {
                player.Attack(enemy);
            }
        }

        public void EnemyMove()
        {
            GameUI.WriteText("Враги атакуют!", ConsoleColor.Red, true);
            Console.WriteLine();

            IsEnemyMove = false;
            IsPlayerMove = true;

            foreach (var enemy in managerWaves.Enemies)
            {
                enemy.MakeDamage(player);
            }
        }
    }

    class CheckActionManager
    {
        public static void CheckAction(SampleAction sampleAction)
        {
            if (sampleAction.Action != null)
            {
                if (sampleAction.AnyKey == false)
                {
                    while (true)
                    {
                        ConsoleKey key = Console.ReadKey(true).Key;
                        if (key == sampleAction.ConsoleKey)
                        {
                            sampleAction.Action();
                            break;
                        }
                    }
                }
                else
                {
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            sampleAction.Action();
                            break;
                        }
                    }
                }
            }

            return;
        }
        public static void CheckAction(SampleAction[] sampleAction)
        {
            if (sampleAction != null)
            {
                while(true)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    SampleAction necessaryAction = sampleAction.FirstOrDefault(x => x.ConsoleKey == key) ?? new SampleAction();
                    if (necessaryAction.Action != null)
                    {
                        necessaryAction.Action(); 
                        break;
                    }
                }

                return;
            }
        }
    }
    class SampleAction
    {
        public ConsoleKey ConsoleKey { get; set; }
        public Action? Action { get; set; }
        public bool AnyKey {  get; set; }   
    }

    class ManagerStatistics
    {
        public static SampleStatistics statistics = new SampleStatistics();
        public static List<SampleStatistics> ListStatistics = new();
        public static SampleStatistics GlobalStatistic = new SampleStatistics();
        private static string? jsonStatistics;

        public static void SaveStatistics()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            ListStatistics.Add(statistics);
            jsonStatistics = JsonSerializer.Serialize(ListStatistics, options);
            File.WriteAllText("statistics.json", jsonStatistics);
            statistics = new SampleStatistics();
        }
        public static void ReadStatistics()
        {
            try
            {
                jsonStatistics = File.ReadAllText("statistics.json");
                ListStatistics = JsonSerializer.Deserialize<List<SampleStatistics>>(jsonStatistics) ?? new List<SampleStatistics>();
            }
            catch (FileNotFoundException)
            {
                Console.Clear();
                GameUI.WriteText("Не удалось найти нужный файл с дaннми статистики", ConsoleColor.DarkGray, true);
                Console.WriteLine();
                Console.WriteLine();
                return;
            }
            catch (JsonException)
            {
                Console.Clear();
                GameUI.WriteText("Файл statistics.json поврежден", ConsoleColor.DarkGray, true);
                Console.WriteLine();
                Console.WriteLine();
                return;
            }

            CalculateGlobalStatistics();
        }

        private static void CalculateGlobalStatistics()
        {
            if (ListStatistics.Count == 0)
                return;

            GlobalStatistic.MaxWave =
                ListStatistics.Max(x => x.MaxWave);

            GlobalStatistic.MaxLevel =
                ListStatistics.Max(x => x.MaxLevel);

            GlobalStatistic.EnemiesKilled =
                ListStatistics.Sum(x => x.EnemiesKilled);
        }
    }
    class SampleStatistics
    {
        public int MaxWave {  get; set; }
        public int MaxLevel { get; set; }  
        public int EnemiesKilled { get; set; }

        public void IncreaseEnemiesKilled(Enemy enemy)
        {
            EnemiesKilled++;
        }
    }

    class PlayerConfig
    {
        public int Health { get; set; }
        public int Damage { get; set; }
        public int XpForNextLevel { get; set; } 
    }
}   
