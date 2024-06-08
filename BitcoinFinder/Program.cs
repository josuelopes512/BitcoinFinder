using BitcoinFinder;
using NBitcoin;
using NBitcoin.DataEncoders;
using System.Globalization;
using System.Numerics;
using System.Text;

class Program
{
    private static HashSet<string> walletsSet = new HashSet<string>(File.ReadAllLines("C:\\Users\\maste\\source\\repos\\BitcoinFinder\\BitcoinFinder\\wallets.txt"));
    private static bool shouldStop = false;
    private static bool isDebug = false;

    static void Main(string[] args)
    {
        Console.Clear();

        Console.WriteLine("\x1b[38;2;250;128;114m" + "╔════════════════════════════════════════════════════════╗\n" +
                          "║" + "\x1b[0m" + "\x1b[36m" + "   ____ _____ ____   _____ ___ _   _ ____  _____ ____   " + "\x1b[0m" + "\x1b[38;2;250;128;114m" + "║\n" +
                          "║" + "\x1b[0m" + "\x1b[36m" + "  | __ )_   _/ ___| |  ___|_ _| \\ | |  _ \\| ____|  _ \\  " + "\x1b[0m" + "\x1b[38;2;250;128;114m" + "║\n" +
                          "║" + "\x1b[0m" + "\x1b[36m" + "  |  _ \\ | || |     | |_   | ||  \\| | | | |  _| | |_) | " + "\x1b[0m" + "\x1b[38;2;250;128;114m" + "║\n" +
                          "║" + "\x1b[0m" + "\x1b[36m" + "  | |_) || || |___  |  _|  | || |\\  | |_| | |___|  _ <  " + "\x1b[0m" + "\x1b[38;2;250;128;114m" + "║\n" +
                          "║" + "\x1b[0m" + "\x1b[36m" + "  |____/ |_| \\____| |_|   |___|_| \\_|____/|_____|_| \\_\\ " + "\x1b[0m" + "\x1b[38;2;250;128;114m" + "║\n" +
                          "║" + "\x1b[0m" + "\x1b[36m" + "                                                        " + "\x1b[0m" + "\x1b[38;2;250;128;114m" + "║\n" +
                          "╚═══════════════\x1b[32m" + "Investidor Internacional - v0.4" + "\x1b[0m\x1b[38;2;250;128;114m══════════╝" + "\x1b[0m");

        Console.Write($"Escolha uma carteira puzzle( {Cyan("1")} - {Cyan("160")}): ");
        string answer = isDebug ? "64" : Console.ReadLine();

        try
        {
            if (int.TryParse(answer, out int walletIndex) && walletIndex >= 1 && walletIndex <= 160)
            {
                List<(BigInteger min, BigInteger max)> ranges = LoadRanges();

                BigInteger min = ranges[walletIndex - 1].min;
                BigInteger max = ranges[walletIndex - 1].max;

                Console.WriteLine($"Carteira escolhida: {Cyan(answer)} Min: {Yellow(min.ToString("x"))} Max: {Yellow(max.ToString("x"))}");
                Console.WriteLine($"Numero possivel de chaves: {Yellow((BigInteger.Parse(max.ToString()) - BigInteger.Parse(min.ToString())).ToString("N0"))}");

                BigInteger key = min;

                Console.Write($"Escolha uma opcao ({Cyan("1")} - Comecar do inicio, {Cyan("2")} - Escolher uma porcentagem, {Cyan("3")} - Escolher minimo): ");
                
                string answer2 = isDebug ? "2" : Console.ReadLine();

                switch (answer2)
                {
                    case "2":
                        BuscaPorPocentagem(max, min);
                        break;
                    case "3":
                        BuscaValorMinimo(max, min);
                        break;
                    default:
                        EncontrarBitcoins(key, min, max);
                        break;
                }
            }
            else
            {
                Console.WriteLine(RedBg("Erro: voce precisa escolher um numero entre 1 e 160"));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            Console.ReadLine();
        }

        Console.CancelKeyPress += (sender, e) => shouldStop = true;
    }

    private static void BuscaPorPocentagem(BigInteger max, BigInteger min)
    {
        BigInteger key = min;

        Console.Write("Escolha um numero entre 0 e 1: ");
        string answer3 = isDebug ? "0.000001" : Console.ReadLine();
        if (decimal.TryParse(answer3, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal percentage) && percentage >= 0 && percentage <= 1)
        {
            BigInteger percentualRange = BigInteger.Divide(
                BigInteger.Multiply(
                    new BigInteger(
                        Math.Floor(percentage * Convert.ToDecimal(1e18))
                    ),
                    BigInteger.Subtract(max, min)
                ),
                new BigInteger(1e18m)
            );

            min = BigInteger.Add(min, percentualRange);

            Console.WriteLine($"Comecando em: {Yellow("0x" + min.ToString("X"))}");

            key = min;
            EncontrarBitcoins(key, min, max);
        }
        else
        {
            Console.WriteLine(RedBg("Erro: voce precisa escolher um numero entre 0 e 1"));
        }
    }

    private static void BuscaValorMinimo(BigInteger max, BigInteger min)
    {
        BigInteger key = min;

        Console.Write("Entre o minimo: ");

        string answer3 = Console.ReadLine();

        if (BigInteger.TryParse(answer3, out BigInteger newMin))
        {
            min = newMin;
            key = newMin;
            EncontrarBitcoins(key, min, max);
        }
        else
        {
            Console.WriteLine(RedBg("Erro: minimo invalido"));
        }
    }

    private static List<(BigInteger min, BigInteger max)> LoadRanges()
    {
        return new RangeConvert().rangesList;
    }

    private static string Cyan(string text) => $"\x1b[36m{text}\x1b[0m";
    private static string Yellow(string text) => $"\x1b[33m{text}\x1b[0m";
    private static string RedBg(string text) => $"\x1b[41m{text}\x1b[0m";

    private static void EncontrarBitcoins(BigInteger key, BigInteger min, BigInteger max)
    {
        int segundos = 0;
        BigInteger pkey = 0;
        var startTime = DateTime.Now;
        var zeroes = Enumerable.Repeat("", 65).ToArray();

        for (int i = 1; i < 64; i++)
        {
            zeroes[i] = new string('0', 64 - i);
        }

        Console.WriteLine("Buscando Bitcoins...");

        while (!shouldStop)
        {
            key++;
            pkey = key; // BigInteger.Parse(key.ToString("X"), System.Globalization.NumberStyles.HexNumber);
            string pkeyHex = pkey.ToString("X");

            string walletPrivateKey = $"{zeroes[pkey.ToString().Length]}{pkeyHex}";
            
            pkey = BigInteger.Parse(zeroes[pkey.ToString().Length] + pkeyHex, System.Globalization.NumberStyles.HexNumber);

            //int seconds = (DateTime.Now - startTime).Milliseconds;

            //if (seconds > segundos)
            //{
            //    segundos += 1000;
            //    Console.WriteLine(segundos / 1000);
            //    if (segundos % 10000 == 0)
            //    {
            //        var tempo = (DateTime.Now - startTime).Milliseconds;
            //        Console.Clear();
            //        Console.WriteLine("Resumo: ");
            //        Console.WriteLine($"Velocidade: {(double)(key - min) / tempo} chaves por segundo");
            //        Console.WriteLine($"Chaves buscadas: {(key - min).ToString("N0")}");
            //        Console.WriteLine($"Ultima chave tentada: {pkey}");

            //        const string filePath = "Ultima_chave.txt";
            //        string content = $"Ultima chave tentada: {pkey}";
            //        try
            //        {
            //            File.WriteAllText(filePath, content, Encoding.UTF8);
            //        }
            //        catch (Exception ex)
            //        {
            //            Console.WriteLine($"Erro ao escrever no arquivo: {ex.Message}");
            //        }
            //    }
            //}

            while(walletPrivateKey.Length < 64)
            {
                walletPrivateKey = $"0{walletPrivateKey}";
            }

            string privateKeyHex = $"{walletPrivateKey}";

            string publicKey = GeneratePublic(privateKeyHex);

           // Console.WriteLine($"0x{walletPrivateKey} -- {publicKey} -- {walletsSet.Contains(publicKey).ToString()}");

            if (walletsSet.Contains(publicKey))
            {
                var tempo = (DateTime.Now - startTime).TotalSeconds;
                Console.WriteLine($"Velocidade: {(double)(key - min) / tempo} chaves por segundo");
                Console.WriteLine($"Tempo: {tempo} segundos");
                Console.WriteLine($"Private key: {Green(privateKeyHex)}");
                Console.WriteLine($"WIF: {Green(GenerateWIF(privateKeyHex))}");

                const string filePath = "keys.txt";
                string lineToAppend = $"Private key: {pkey}, WIF: {GenerateWIF(privateKeyHex)}\n";
                try
                {
                    File.AppendAllText(filePath, lineToAppend, Encoding.UTF8);
                    Console.WriteLine("Chave escrita no arquivo com sucesso.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao escrever chave no arquivo: {ex.Message}");
                }

                throw new Exception("ACHEI!!!! 🎉🎉🎉🎉🎉");
            }
        }
    }

    private static string GeneratePublic(string privateKey)
    {
        Key privateKeydata = new Key(Encoders.Hex.DecodeData(privateKey), fCompressedIn: true);

        BitcoinAddress publicAddress = privateKeydata.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main);
        
        return publicAddress.ToString();
    }

    private static string GenerateWIF(string privateKey)
    {
        Key privateKeydata = new Key(Encoders.Hex.DecodeData(privateKey), fCompressedIn: true);

        BitcoinSecret bitcoinSecret = privateKeydata.GetBitcoinSecret(Network.Main);

        return bitcoinSecret.ToWif();
    }

    private static string Green(string text) => $"\x1b[32m{text}\x1b[0m";
}
