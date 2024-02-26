using System.Drawing;

namespace SnakeGame.ConsoleApp;

public class Snake
{
    #region Constants
    //yılan array olabilir 00000 gibi yem yicez bi halka daha ekliyorum
    // 000000>
    //  000000>
    //   000000>  //ekleme ve çıkarma işlemlerinin hızlı olabilmesi için buna uygun veriyapısı kullanmamız gerekir link list tutacağız

    /*
     * 
     00000
         0
         v  olmuşta olabilir

     */
    private const int DEFAULT_DELAY = 75; //her 75 milisaniyede snake hareket etsin
    private const int DEFAULT_SNAKE_LENGTH = 5;
    private const double DEFAULT_VERTICAL_DELAY_RATIO = 0.6;
    private const double delay = DEFAULT_DELAY;
    private const double delayBoost = DEFAULT_DELAY / 2; //37,5 milisaniyede yılan hareket etsin

    private const ConsoleColor FOOD_COLOR = ConsoleColor.Green;
    private const ConsoleColor SNAKE_COLOR = ConsoleColor.Yellow;
    private const char DEFAULT_SNAKE_CHAR = 'O';
    //kütüphanenin içinden rectangle alalım
    private const char DEFAULT_BORDER_CHAR = '#'; //border
    private const char DEFAULT_FOOD_CHAR = '*'; //yem

    private const Direction DEFAULT_DIRECTION = Direction.Right;

    private static Point DEFAULT_SNAKE_POINT = Point.Empty;

    private static Rectangle DEFAULT_BORDER_REC = new(1, 1, 50, 20);

    #endregion

    #region Private Variables

    private LinkedList<Point> segments;
    private char snakeChar;
    private HashSet<Point> emptyPoints;

    private Point food, snakeStartingPoint;
    private int foodEaten = 0, initialSnakeLength;
    private Direction currentDirection;
    private Rectangle borderRec;

    private char borderChar, foodChar;

    private bool speedBoosted = false,
                 isPaused = false;

    #endregion

    #region Constructors

    public Snake()
    {
        SetDefaults();
    }

    public Snake(char snakeChar,
               Point snakePoint,
               int snakeInitialLength,
               Rectangle borderRec)
    {
        this.borderRec = borderRec;
        this.snakeChar = snakeChar;
        snakeStartingPoint = snakePoint;
        initialSnakeLength = snakeInitialLength;
    }

    #endregion
    public async Task Run(CancellationToken token = default)
    {
        Adjust();
        PrintBorder();
        PrintFood(food.X, food.Y); // initial food xine ve y sine git bunu çiz

        while (!token.IsCancellationRequested) //sonsuz döngü başladı oyun bitmemesi için yılan sürekli hareket ediyo
        {
            WaitForKeyPressAndSetDirectionAndDelay(); //sen öncelikle bi tuşa basıldı mı diye bekle
            PrintStatusBar(); //yılanın hareket ettiğindeki değişkenleri yazıdırıyorum

            // save old head and tail 000000> bir sağa kaydırabilmek için kuyruğa gidip 00000> çizdireceğiz sağa doğru kaydırmış oluyoruz
            var oldTail = segments.Last.Value;  
            var oldHead = segments.First.Value;

            // head to the new position
            var x = currentDirection == Direction.Right ? 1 : currentDirection == Direction.Left ? -1 : 0; //yılanın en son halini getir(currentDirection) sağa mı sola mı gidiyoyu buluyoruz: eğer sağa veya sola gidiyosa ya 1 olsun yada -1 olsun
            var y = currentDirection == Direction.Down ? 1 : currentDirection == Direction.Up ? -1 : 0;
            var head = new Point(oldHead.X + x,
                                 oldHead.Y + y);

            segments.AddFirst(head); //ekliyorum yılanın başına 00000>> ekledim

            if (head == food) // Eat the food eğer benim headim boyu foodum boyuyla eşitse veya bir yemek bulduysam burada
            {
                await CreateFood(token); //food oluştur
                PrintFood(food.X, food.Y); //ekrana çizdir foodu

                foodEaten++; //kaç tane yem yedim
                PrintStatusBar(); //status barı yenile
                emptyPoints.Add(food); // add the food point to empty points 
            }
            else
            {
                segments.RemoveLast(); //segmentimin en son karakterini temizle
                ConsoleHelper.ClearText(oldTail.X, oldTail.Y); // remove old tail oldTaili temizle
            }

            DrawSnakeHead(head, currentDirection); //up mu down mı bakıp içine çizecek
            // replace old head with snakeChar
            ConsoleHelper.ResetCursorPosition(oldHead.X, oldHead.Y); //nereye göndereceğim ?
            Console.Write(snakeChar); //overide et 

            // Check for collision with walls or itself
            bool hasCollision = HasCollision(head.X, head.Y); //bi yere çarptım mı çarpmadım mı ?
            if (hasCollision) //eğer hasCollision ise
            {
                await PrintGameOver(token); //
                break; //kır çık
            }

            await Task.Delay((int)GetDelay(), token); //olmadıysa yılan hareket ediyor demektir ne 
        }
    }

    private void Adjust()
    {
        segments = new LinkedList<Point>();

        for (int i = 0; i < initialSnakeLength; i++)
        {
            segments.AddLast(new Point(borderRec.Left + snakeStartingPoint.X - i,
                                       borderRec.Top + snakeStartingPoint.Y));
        }

        // inital segment excluding
        emptyPoints = new HashSet<Point>(borderRec.Width * borderRec.Height);
        for (int x = borderRec.Left + 1; x < borderRec.Right - 1; x++)
        {
            for (int y = borderRec.Top + 1; y < borderRec.Bottom - 1; y++)
            {
                emptyPoints.Add(new Point(x, y));
            }
        }

        emptyPoints.ExceptWith(segments);

        CreateFood().GetAwaiter().GetResult();
    }
    private void SetDefaults() //method oluşturdum ve değişkenlerimin default değerlerine bunları atadım
    { 
        borderRec = DEFAULT_BORDER_REC; //borderimın rectanglesi ne kadar büyüklükte bir değişken
        snakeChar = DEFAULT_SNAKE_CHAR; //yılanı hangi karakterlerden oluşturacağız
        initialSnakeLength = DEFAULT_SNAKE_LENGTH; //yılan ilk oynamaya başladığımızda kaç karakter uzunluğunda olacak
        snakeStartingPoint = new Point(1, 1); //hangi noktadan başlasın
        currentDirection = DEFAULT_DIRECTION; //yılanın yönünü belirliyoruz
        foodChar = DEFAULT_FOOD_CHAR; //hangi karakterden oluşacak
        borderChar = DEFAULT_BORDER_CHAR; //hangi karakterden oluşacak
    }

    private static void DrawSnakeHead(Point position, Direction currentPosition) //istediğimiz pozisyona götürüp cursora çiziyoruz
    {
        ConsoleHelper.ResetCursorPosition(position.X, position.Y);

        var headChar = currentPosition switch //yukarı aşağı sağa sola giderken yılanın yönünü belli etmesi için switch
        {
            Direction.Up => '^',
            Direction.Down => 'v',
            Direction.Left => '<',
            Direction.Right => '>',
            _ => throw new NotImplementedException()
        };

        Console.Write(headChar); //ekrana yazıyoruz headChar'ı
    }

    private double GetDelay()
    {
        var defaultDelay = speedBoosted ? delayBoost : delay;
        return currentDirection == Direction.Left || currentDirection == Direction.Right
            ? defaultDelay
            : defaultDelay / DEFAULT_VERTICAL_DELAY_RATIO;
    }

    private bool HasCollision(int x, int y) //yılanın border içinde kalacağından emin oluyoruz
    {
        return x == borderRec.X //x borderin x'ine ulaştı mı ?
                || x == borderRec.Right //yada en sağ kısmına ulaştı mı ?
                || y == borderRec.Y //yada en sol kısmına ulaştı mı ?
                || y == borderRec.Bottom //en alt kısmına ulaştı mı ?
                || segments.Skip(1).Any(i => i.X == x && i.Y == y); //sadece kenarlara ulaştığında oyun bitmiyo kendine çarptığında da oyun bitiyo
    }

    private void WaitForKeyPressAndSetDirectionAndDelay() //basılan tuşları dinleyip aksiyon alıyor
    {
        if (!Console.KeyAvailable)
            return;

        var key = Console.ReadKey(true).Key;

        if (key == ConsoleKey.P) //p ye basılmış ise şunlar
        {
            isPaused = !isPaused;
            if (isPaused)
            {
                string pauseMessage = "PAUSED. Press ANY key to continue";
                PrintStatusBarWithMessage(pauseMessage); //status bara basıyoruz
                Console.ReadKey(true); //herhangi bir tuşa basıldı
                isPaused = false;
            }
        }
        else if (key == ConsoleKey.UpArrow) //yukarı tuşları
        {
            currentDirection = Direction.Up; //directionu değiştir
        }
        else if (key == ConsoleKey.DownArrow)
        {
            currentDirection = Direction.Down;
        }
        else if (key == ConsoleKey.LeftArrow)
        {
            currentDirection = Direction.Left;
        }
        else if (key == ConsoleKey.RightArrow)
        {
            currentDirection = Direction.Right;
        }
        else if (key == ConsoleKey.Spacebar) //aç kapat kapat aç
        {
            speedBoosted = !speedBoosted;
        }
    }

    private void PrintBorder()
    {
        // Draw border
        for (int i = borderRec.X; i < borderRec.Right; i++)
        {
            ConsoleHelper.ResetCursorPosition(i, borderRec.Y); // first line
            Console.Write(DEFAULT_BORDER_CHAR);
            ConsoleHelper.ResetCursorPosition(i, borderRec.Bottom); // last line
            Console.Write(DEFAULT_BORDER_CHAR);
        }

        for (int i = borderRec.Y; i < borderRec.Y + borderRec.Height; i++)
        {
            ConsoleHelper.ResetCursorPosition(borderRec.X, i); // first column
            Console.Write(DEFAULT_BORDER_CHAR);
            ConsoleHelper.ResetCursorPosition(borderRec.Right, i); // last column
            Console.Write(DEFAULT_BORDER_CHAR);
        }
    }

    #region Game Over/Completed Methods

    private async Task PrintComplete(CancellationToken token = default) //öncelikle tebrikler 
    {
        Console.ForegroundColor = ConsoleColor.Green; //içini yeşil yapıyorum
        PrintBorder();
        PrintStatusBar();

        var message = "Completed!";

        var middle = GetCenterOfBorder(message.Length / 2); //borderin tam ortasına

        await ConsoleHelper.PrintBlinkingText(message, middle, 500, token); //while döngüsü içersinde ekrana sürekli yansıyo
    }

    private async Task PrintGameOver(CancellationToken token = default) 
    {
        Console.ForegroundColor = ConsoleColor.Red;
        PrintBorder();
        PrintStatusBar();

        var message = "Game Over!";

        var middle = GetCenterOfBorder(message.Length / 2);

        await ConsoleHelper.PrintBlinkingText(message, middle, delay: 500, token);
    }

    #endregion

    #region StatusBar Methods

    private void PrintStatusBar() //ekranın en üstüne yılanın durumunu gösterir
    {
        var message = string.Format("Eaten: {0}, L: {1}, S: {2}, Dir: {3}, Delay: {4}, Food: {5}, Head: {6}",
                                foodEaten,
                                segments.Count,
                                borderRec.Width + "x" + borderRec.Height,
                                currentDirection.ToString(),
                                GetDelay().ToString("#"),
                                $"{borderRec.X + food.X},{borderRec.Y + food.Y}",
                                $"{segments.First.Value.X},{segments.First.Value.Y}");

        if (isPaused) //p harfine basınca oyun durur
            message += " Status: PAUSED";

        if (speedBoosted) //space ye basınca yılan hızlanıyor
            message += " SPEED BOOST";

        PrintStatusBarWithMessage(message);
    }

    private void PrintStatusBarWithMessage(string message) //oyunu durdurma işlemi yapan metod
    {
        ConsoleHelper.ClearLine(y: 0); //statusbarımız ilk satırda olacak komutu
        ConsoleHelper.ResetCursorPosition(borderRec.Left); //
        Console.Write(message); //sürekli üzerine yazıyoruz
    }

    #endregion

    #region Food Methods

    private async Task CreateFood(CancellationToken token = default) //yem oluşturacağız, empty
    {
        if (emptyPoints.Count == 0)
        {
            await PrintComplete(token);
            return;
        }

        var randomIndex = Random.Shared.Next(0, emptyPoints.Count); //sharedin içersinden random bi eleman oluşturdum ekliyoruz
        food = emptyPoints.ElementAt(randomIndex);
        emptyPoints.Remove(food);
    }

    private void PrintFood(int x, int y)  //yemi çizdireceğiz
    {
        var currentColor = Console.ForegroundColor;
        ConsoleHelper.ResetCursorPosition(x, y);
        Console.ForegroundColor = FOOD_COLOR;
        Console.Write(foodChar);
        Console.ForegroundColor = currentColor;
    }

    #endregion

    #region Helper Methods

    private Point GetCenterOfBorder(int xOffSet = 0, int yOffSet = 0)
    {
        return new Point((borderRec.Left + borderRec.Right) / 2 - xOffSet,
                         (borderRec.Top + borderRec.Bottom) / 2 - yOffSet);
    }

    #endregion

    #region Property Set Methods

    public void SetSnakeChar(char snakeChar)
    {
        this.snakeChar = snakeChar;
    }

    public void SetSnakeStartingPoint(Point snakeStartingPoint)
    {
        this.snakeStartingPoint = snakeStartingPoint;
    }

    public void SetInitialSnakeLength(int initialSnakeLength)
    {
        this.initialSnakeLength = initialSnakeLength;
    }

    public void SetCurrentPosition(Direction currentPosition)
    {
        this.currentDirection = currentPosition;
    }

    public void SetBorderRec(Rectangle borderRec)
    {
        this.borderRec = borderRec;
    }

    public void SetBorderChar(char borderChar)
    {
        this.borderChar = borderChar;
    }

    public void SetFoodChar(char foodChar)
    {
        this.foodChar = foodChar;
    }

    #endregion
}