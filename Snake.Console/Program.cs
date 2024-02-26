/*yılan oyunu için bir border çizeceğiz ve (snake sınıf, metodla) oluşturalım
yılan nesnesi oluşturacağız ve borderların içersinde gezecek duvara çarptığında yanacak
duvara çarpmadığında sürekli hareket edecek sürekli bir hareket olduğu için bizim bunu while döngü içersinde belli bir milisaniye
de çalıştıracağız*/

using SnakeGame.ConsoleApp;
using System.Drawing;


Console.CursorVisible = false;

int borderWidth = 50, borderHeigth = 20; //border genişlik ve yüksekliği
int borderX = 20, borderY = 5; //rectangle uzunluğu

var message = "Press any key to start the game"; //bir tuşa bas

Console.SetCursorPosition((Console.WindowWidth - message.Length) / 2, Console.CursorTop + 5);
Console.WriteLine(message);

Console.ReadKey();
Console.Clear();

Snake snake = new(snakeChar: 'O',
                  snakePoint: new Point(5, 5), //rectangle içinde 5-5 noktasından başlasın
                  snakeInitialLength: 2,
                  new Rectangle(borderX, borderY, borderWidth, borderHeigth));

snake.SetSnakeChar('■');
snake.SetInitialSnakeLength(5);
snake.SetFoodChar('■');

await snake.Run();
