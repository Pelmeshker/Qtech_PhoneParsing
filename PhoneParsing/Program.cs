using RestSharp;
using System.Text;

string writePath = @"IpPhoneInfo.txt";
FileInfo fi1 = new FileInfo(writePath);
var counter = 0;

//Сразу проверка существования текстового файла и его создание в противном случае
if (!fi1.Exists)
{
    using (FileStream fs = File.Create(writePath))
    {
        byte[] info = new UTF8Encoding(true).GetBytes("");
        fs.Write(info, 0, info.Length);
    }
}

//Очистка текстового файла от прошлых проверок
File.WriteAllText(writePath, String.Empty);

//Проходим каждый аппарат подсети по очереди
for (int i = 1; i < 255; i++)
{
    //Если набралось больше 10 неудач подряд, то останавливаем цикл, дальше устройств точно не будет
    //DHCP выдает телефонам адреса по порядку, поэтому такое решение не несет в себе рисков
    if (counter < 10)
    {
        CheckAviability("http://192.168.1." + i + "/SysStatus.asp");
    } else
    {
        break;
    }
}

//Здесь проверяется, отвечает ли нам устройство отказом в авторизации по заданной странице
//Если да, то имеет смысл начинать парсить по-настоящему
void CheckAviability(string address)
{
    Console.WriteLine("Сейчас проверяется " + address);
    var client = new RestClient(address);
        client.Timeout = -1;
        var request = new RestRequest(Method.GET);
        IRestResponse response = client.Execute(request);
        if (response.Content.Contains("Document Error: Unauthorized"))
        {
            if (FindAndWriteResult(address) == false)
        {
            counter = 0;
            FindAndWriteResult(address);
        } 

        } else
    {
        Console.WriteLine("По этому адресу телефон не найден");
        counter++;
    }
}

//Метод фактического парсинга и записи нужных нам полей в текстовый файл
bool FindAndWriteResult(string address)
{
    
    var client = new RestClient(address);
    String mac, ip, phone, ver, result;

    //Авторизация и получение страницы, используется уже готовый заголовок
    client.Timeout = -1;
    var request = new RestRequest(Method.GET);
    request.AddHeader("Upgrade-Insecure-Requests", "1");
    client.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36";
    request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
    request.AddHeader("Authorization", "Basic YWRtaW46YWRtaW4=");
    IRestResponse response = client.Execute(request);
    var contentString = response.Content;

    //Обрезка строки до ключевого слова и поиск первого тега <td> после него, запись значения в переменную
    try
    {

        contentString = contentString.Substring(contentString.IndexOf("MAC Address"));
        contentString = contentString.Substring(contentString.IndexOf("<td>") + 4);
        mac = contentString.Substring(0, (contentString.IndexOf("</td>")));

        contentString = contentString.Substring(contentString.IndexOf("IP"));
        contentString = contentString.Substring(contentString.IndexOf("<td>") + 4);
        ip = contentString.Substring(0, (contentString.IndexOf("</td>")));

        contentString = contentString.Substring(contentString.IndexOf("Phone Model"));
        contentString = contentString.Substring(contentString.IndexOf("<td width=") + 16);
        phone = contentString.Substring(0, (contentString.IndexOf("</td>")));

        contentString = contentString.Substring(contentString.IndexOf("Software Version"));
        contentString = contentString.Substring(contentString.IndexOf("<td width=") + 16);
        ver = contentString.Substring(0, (contentString.IndexOf("</td>")));

        result = mac + "  " + ip + "  " + phone + "  " + ver;
        Console.WriteLine(result);


        //Запись результата в файл

        try
        {
            using (StreamWriter sw = new StreamWriter(writePath, true, System.Text.Encoding.Default))
            {
                sw.WriteLine(result);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);

        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        return false;
    }

    return true;
   
}

