using BitcoinFinder;
using NBitcoin;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using Utils = BitcoinFinder.Utils;

class Program
{
    //private static HashSet<string> walletsSet = new HashSet<string>(File.ReadAllLines("C:\\Users\\maste\\source\\repos\\BitcoinFinder\\BitcoinFinder\\wallets.txt"));
    private static bool shouldStop = false;
    private static bool isDebug = false;
    private static CancellationTokenSource cancellationTokenSource;
    private static ConcurrentDictionary<BigInteger, bool> checkedKeys = new ConcurrentDictionary<BigInteger, bool>();
    private static ConcurrentBag<string> walletsSet = new ConcurrentBag<string>(File.ReadAllLines("C:\\Users\\maste\\source\\repos\\BitcoinFinder\\BitcoinFinder\\wallets.txt")); // Conjunto de endereços públicos para verificação

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

        Console.Write($"Escolha uma carteira puzzle( {Cyan("1")} - {Cyan("160")}  Puzzle (66)):  ");
        string answer = isDebug ? "64" : Console.ReadLine();

        try
        {
            if (int.TryParse(answer, out int walletIndex) && walletIndex >= 1 && walletIndex <= 160)
            {
                var ranges = LoadRanges();

                (BigInteger min, BigInteger max) range = ranges[walletIndex - 1]; // 590295810358705651712

                Console.WriteLine($"Carteira escolhida: {Cyan(answer)} Min: {Yellow(range.min.ToString("x"))} Max: {Yellow(range.max.ToString("x"))}");
                Console.WriteLine($"Numero possivel de chaves: {Yellow((BigInteger.Parse(range.max.ToString()) - BigInteger.Parse(range.min.ToString())).ToString("N0"))}");
                Console.Write($"Escolha uma opcao \n" +
                    $"({Cyan("1")} - Comecar do inicio, \n" +
                    $"{Cyan("2")} - Escolher uma porcentagem, \n" +
                    $"{Cyan("3")} - Escolher minimo, \n" +
                    $"{Cyan("4")} - Força Bruta Com Batchs, \n" +
                    $"{Cyan("5")} - Força Bruta Com Threads, \n" +
                    $"{Cyan("6")} - Força Bruta Com Threads e Sorteio, \n" +
                    $"{Cyan("7")} - Threads e Sorteio): ");
                
                string answer2 = isDebug ? "2" : Console.ReadLine();

                switch (answer2)
                {
                    case "1":
                        EncontrarBitcoins(range.min, range);
                        break;
                    case "2":
                        BuscaPorPocentagem(range);
                        break;
                    case "3":
                        BuscaValorMinimo(range.max);
                        break;
                    case "4":
                        EncontrarBitcoinsBatchs(range.min, range);
                        break;
                    case "5":
                        //CheckRange(range, Console.ReadLine());
                        Search(range);
                        break;
                    case "6":
                        //CheckRange(range, Console.ReadLine());
                        Search(range, 1000, 1000);
                        break;
                    case "7":
                        bool stopRange = false;

                        Parallel.Invoke(
                            () =>
                            {
                                while (!shouldStop && !stopRange)
                                {
                                    if (CheckRandomRange(range, stopRange))
                                    {
                                        stopRange = true;
                                    }
                                }
                            }
                        );
                        break;
                    default:
                        cancellationTokenSource = new CancellationTokenSource();
                        BuscaParalela(range);
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

        Console.CancelKeyPress += (sender, e) => shouldStop = true;
    }

    private static bool EncontrarBitcoinsBatchs(BigInteger key, (BigInteger min, BigInteger max) range, int batchSize = 10000)
    {
        while (key <= range.max)
        {
            var zeroes = Enumerable.Repeat("", 65).ToArray();

            for (int i = 1; i < 64; i++)
            {
                zeroes[i] = new string('0', 64 - i);
            }

            List<string> privateKeysBatch = new List<string>();

            for (int i = 0; i < batchSize; i++)
            {
                if (key >= range.max)
                    break;

                key++;
                string pkeyHex = key.ToString("X");
                string walletPrivateKey = $"{zeroes[key.ToString().Length]}{pkeyHex}";

                while (walletPrivateKey.Length < 64)
                {
                    walletPrivateKey = $"0{walletPrivateKey}";
                }

                privateKeysBatch.Add(walletPrivateKey);
            }

            ParallelOptions options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(privateKeysBatch, options, privateKeyHex =>
            {
                Key privateKey = new Key(Encoders.Hex.DecodeData(privateKeyHex), fCompressedIn: true);
                BitcoinAddress publicAddress = privateKey.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main);

                if (walletsSet.Contains(publicAddress.ToString()))
                {
                    Console.WriteLine($"\n\n\n{Yellow("=====================================================")}\n\n");
                    Console.WriteLine($"{RedBg("ENDERECO ENCONTRADO")} : {Cyan(publicAddress.ToString())}");
                    Console.WriteLine($"{RedBg("CHAVE PRIVADA")} : {Cyan(privateKeyHex)}\n");
                    Console.WriteLine($"\n\n{Yellow("=====================================================")}\n\n");
                }

                return;
            });
        }

         return false;
    }


    private static void BuscaParalela((BigInteger min, BigInteger max) ranges)
    {
        BigIntegerRange partitioner = new BigIntegerRange(ranges.min, ranges.max);
        ParallelOptions options = new ParallelOptions { 
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationTokenSource.Token
        };

        Parallel.ForEach(
            PartitionBigInteger.Create(ranges.min, ranges.max),
            //partitioner,
            options,
            //key =>
            //{
            //    if (cancellationTokenSource.Token.IsCancellationRequested)
            //        return;

            //    if (EncontrarBitcoins(key, ranges))
            //    {
            //        cancellationTokenSource.Cancel(); // Cancel other threads
            //    }

            //    //EncontrarBitcoins(key, ranges);
            //}
            range =>
            {
                BigInteger chunkMin = range.Item1;
                BigInteger chunkMax = range.Item2;

                BigInteger key = chunkMin;

                while (!shouldStop && key <= chunkMax)
                {
                    if (EncontrarBitcoins(key, ranges))
                    {
                        return;
                    }

                    //if (EncontrarBitcoinsBatchs(key, range))
                    //{
                    //    return;
                    //}

                    key++;
                }
            }
            );
    }

    private static void BuscaPorPocentagem((BigInteger min, BigInteger max) range)
    {
        Console.Write("Escolha um numero entre 0 e 1: ");
        string answer3 = isDebug ? "0.000001" : Console.ReadLine();

        if (decimal.TryParse(answer3, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal percentage) && percentage >= 0 && percentage <= 1)
        {
            BigInteger percentualRange = BigInteger.Divide(
                BigInteger.Multiply(
                    new BigInteger(
                        Math.Floor(percentage * Convert.ToDecimal(1e18))
                    ),
                    BigInteger.Subtract(range.max, range.min)
                ),
                new BigInteger(1e18m)
            );

            BigInteger min = BigInteger.Add(range.min, percentualRange);

            Console.WriteLine($"Comecando em: {Yellow("0x" + min.ToString("X"))}");

            (BigInteger min, BigInteger max) newrange = new ()
            {
                min = min,
                max = range.max
            };

            //EncontrarBitcoinsBatchs(min, newrange);

            Search(newrange);

            //EncontrarBitcoins(min, newrange);
        }
        else
        {
            Console.WriteLine(RedBg("Erro: voce precisa escolher um numero entre 0 e 1"));
        }
    }

    private static void BuscaValorMinimo(BigInteger max)
    {
        Console.Write("Entre o minimo: ");

        string answer3 = Console.ReadLine();

        if (BigInteger.TryParse(answer3, out BigInteger newMin))
        {
            (BigInteger min, BigInteger max) newrange = new()
            {
                min = newMin,
                max = max
            };

            EncontrarBitcoins(newMin, newrange);
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

    private static bool EncontrarBitcoins(BigInteger key, (BigInteger min, BigInteger max) range)
    {
        int segundos = 0;
        BigInteger pkey = 0;
        var startTime = DateTime.Now;
        var zeroes = Enumerable.Repeat("", 65).ToArray();

        for (int i = 1; i < 64; i++)
        {
            zeroes[i] = new string('0', 64 - i);
        }

        //Console.WriteLine("Buscando Bitcoins...");

        while (!shouldStop && pkey <= range.max)
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

            string publicKey = GeneratePublic(new Key(Encoders.Hex.DecodeData(privateKeyHex), fCompressedIn: true));

            // Console.WriteLine($"0x{walletPrivateKey} -- {publicKey} -- {walletsSet.Contains(publicKey).ToString()}");

            if (walletsSet.Contains(publicKey))
            {
                Key privK = new Key(Encoders.Hex.DecodeData(privateKeyHex), fCompressedIn: true);

                var tempo = (DateTime.Now - startTime).TotalSeconds;
                Console.WriteLine($"Velocidade: {(double)(key - range.min) / tempo} chaves por segundo");
                Console.WriteLine($"Tempo: {tempo} segundos");
                Console.WriteLine($"Private key: {Green(privateKeyHex)}");
                Console.WriteLine($"WIF: {Green(GenerateWIF(privK))}");
                Console.WriteLine($"Saldo: {Green(Utils.GetBitcoinBalance(publicKey).Result.ToString())}");
                

                const string filePath = "keys.txt";
                string lineToAppend = $"Private key: {pkey}, WIF: {GenerateWIF(privK)}\n";
                try
                {
                    File.AppendAllText(filePath, lineToAppend, Encoding.UTF8);
                    Console.WriteLine("Chave escrita no arquivo com sucesso.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao escrever chave no arquivo: {ex.Message}");
                }

                shouldStop = true;

                //throw new Exception("ACHEI!!!! 🎉🎉🎉🎉🎉");

                //cancellationTokenSource.Cancel();

                return true;
            }
        }

        return false;
    }

    private static string GeneratePublic(Key privateKey)
    {       
        return privateKey.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main).ToString();
    }

    private static string GenerateWIF(Key privateKey)
    {
        return privateKey.GetBitcoinSecret(Network.Main).ToWif();
    }

    public static void Search((BigInteger min, BigInteger max) ranges)
    {
        var stopwatch = Stopwatch.StartNew();

        List<(BigInteger, BigInteger)> dataRange = BigIntegerRange.DivideRangeList(ranges.min, ranges.max, Environment.ProcessorCount);

        ParallelOptions options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        Parallel.ForEach(dataRange, options, rangePartititon =>
        {
            if (!shouldStop)
            {
                CheckRange(new() { min = rangePartititon.Item1, max = rangePartititon.Item2 });
            }
            else 
                return;
        });

        stopwatch.Stop();

        Console.WriteLine($"Tempo de execução: {stopwatch.Elapsed}");
    }

    private static void CheckRange((BigInteger min, BigInteger max) range)
    {
        for (BigInteger key = range.min; key <= range.max; key++)
        {
            if (shouldStop)
                return;

            if (CheckAddress(key, out string privkey))
            {
                Console.WriteLine($"Private key found: {privkey}");
                Console.WriteLine($"WIF: {Green(GenerateWIF(new Key(Encoders.Hex.DecodeData(privkey), fCompressedIn: true)))}");
                Console.WriteLine($"Bitcoin address found in range {key.ToString("x")}");

                shouldStop = true;

                return;
            }
        }

        return;
    }

    private static bool CheckAddress(BigInteger key, out string privkey)
    {
        privkey = AddZeroes(key.ToString("X"));

        return walletsSet.Contains(GeneratePublic(new Key(Encoders.Hex.DecodeData(privkey), fCompressedIn: true)));
    }

    private static string AddZeroes(string key)
    {
        StringBuilder sb = new StringBuilder(64);

        for (int i = 0; i < (64 - key.Length); i++)
        {
            sb.Append('0');
        }

        sb.Append(key);

        return sb.ToString();
    }

    private static void CheckSequentialRange((BigInteger min, BigInteger max) range)
    {
        bool stopRange = false;

        Parallel.Invoke(
            () =>
            {
                while (!shouldStop && !stopRange)
                {
                    if (CheckRandomRange(range, stopRange))
                    {
                        stopRange = true;
                    }
                }
            },
            () =>
            {
                while (!shouldStop && !stopRange)
                {
                    if (CheckRandomRange(range, stopRange))
                    {
                        stopRange = true;
                    }
                }
            },
            () =>
            {
                while (!shouldStop && !stopRange)
                {
                    if (CheckRandomRange(range, stopRange))
                    {
                        stopRange = true;
                    }
                }
            },
            () =>
            {
                while (!shouldStop && !stopRange)
                {
                    if (CheckRandomRange(range, stopRange))
                    {
                        stopRange = true;
                    }
                }
            },
            () =>
            {
                while (!shouldStop && !stopRange)
                {
                    if (CheckRandomRange(range, stopRange))
                    {
                        stopRange = true;
                    }
                }
            }
        );

        for (BigInteger key = range.min; key <= range.max; key++)
        {
            if (shouldStop)
                return;

            if (/*checkedKeys.TryAdd(key, true) &&*/ CheckAddress(key, out string privkey))
            {
                Console.WriteLine($"Private key found: {privkey}");
                Console.WriteLine($"WIF: {GenerateWIF(new Key(Encoders.Hex.DecodeData(privkey), fCompressedIn: true))}");
                Console.WriteLine($"Bitcoin address found in range {key.ToString("x")}");
                //Console.WriteLine($"QR Code: {GenerateQRCode(GenerateWIF(new Key(Encoders.Hex.DecodeData(privkey), fCompressedIn: true)))}");

                shouldStop = true;

                return;
            }
        }

        stopRange = true;
    }

    private static bool CheckRandomRange((BigInteger min, BigInteger max) range, bool stopRange = false)
    {
        BigInteger attemps = 0;

        try
        {
            while (!shouldStop && !stopRange)
            {
                BigInteger randomKey = GenerateRandomKey(range.min, range.max);

                if (/*checkedKeys.TryAdd(randomKey, true) &&*/ CheckAddress(randomKey, out string privkey))
                {
                    Console.WriteLine($"Private key found by Sorted: {privkey}");
                    Console.WriteLine($"Private key found: {privkey}");
                    Console.WriteLine($"WIF: {GenerateWIF(new Key(Encoders.Hex.DecodeData(privkey), fCompressedIn: true))}");
                    Console.WriteLine($"Bitcoin address found in random search {randomKey.ToString("x")}");
                    //Console.WriteLine($"QR Code: {GenerateQRCode(GenerateWIF(new Key(Encoders.Hex.DecodeData(privkey), fCompressedIn: true)))}");

                    shouldStop = true;

                    return true;
                }
                else
                {
                    attemps++;
                }

                if (attemps >= (BigInteger.Subtract(range.max, range.min)))
                {
                    stopRange = true;

                    return false;
                }
            }
        }
        catch (Exception)
        {
            return true;
        }

        return false;
    }

    private static void CheckRange((BigInteger min, BigInteger max) range, int randomAttempts)
    {
        for (BigInteger key = range.min; key <= range.max; key++)
        {
            if (shouldStop)
                return;

            if (checkedKeys.TryAdd(key, true) && CheckAddress(key, out string privkey))
            {
                Console.WriteLine($"Private key found: {privkey}");
                Console.WriteLine($"WIF: {GenerateWIF(new Key(Encoders.Hex.DecodeData(privkey), fCompressedIn: true))}");
                Console.WriteLine($"Bitcoin address found in range {key.ToString("x")}");
                //Console.WriteLine($"QR Code: {GenerateQRCode(GenerateWIF(new Key(Encoders.Hex.DecodeData(privkey), fCompressedIn: true)))}");

                shouldStop = true;

                return;
            }
        }

        // Realizar tentativas aleatórias
        for (int i = 0; i < randomAttempts; i++)
        {
            if (shouldStop)
                return;

            BigInteger randomKey = GenerateRandomKey(range.min, range.max);

            if (checkedKeys.TryAdd(randomKey, true))
            {
                if (CheckAddress(randomKey, out string privkey))
                {
                    Console.WriteLine($"Private key found: {privkey}");
                    Console.WriteLine($"WIF: {GenerateWIF(new Key(Encoders.Hex.DecodeData(privkey), fCompressedIn: true))}");
                    Console.WriteLine($"Bitcoin address found in random search {randomKey.ToString("x")}");
                    //Console.WriteLine($"QR Code: {GenerateQRCode(GenerateWIF(new Key(Encoders.Hex.DecodeData(privkey), fCompressedIn: true)))}");

                    shouldStop = true;

                    return;
                }
            }
        }
    }

    private static BigInteger GenerateRandomKey(BigInteger min, BigInteger max, int maxAttempts = 1000)
    {
        byte[] bytes = max.ToByteArray();
        BigInteger randomKey = min - 1;
        int attempts = 0;
        Random random = new Random();

        while ((randomKey < min || randomKey > max) && attempts < maxAttempts)
        {
            random.NextBytes(bytes);
            bytes[bytes.Length - 1] &= (byte)0x7F; // Para garantir que o número seja positivo
            randomKey = new BigInteger(bytes);

            if (randomKey < BigInteger.Zero)
                randomKey = -randomKey;

            randomKey = randomKey % (max - min + 1) + min;

            attempts++;
        }

        if (attempts >= maxAttempts)
        {
            throw new Exception("Falha ao gerar chave aleatória dentro do intervalo após várias tentativas.");
        }

        return randomKey;
    }

    public static void Search((BigInteger min, BigInteger max) ranges, int numberOfBlocks = 1000, int randomAttempts = 1000)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        int numberOfProcessors = Environment.ProcessorCount;

        List<(BigInteger, BigInteger)> dataRange = BigIntegerRange.DivideRangeList(ranges.min, ranges.max, numberOfProcessors);

        ParallelOptions options = new ParallelOptions
        {
            MaxDegreeOfParallelism = numberOfProcessors
        };

        bool stopRange = false;

        Parallel.Invoke(
            () =>
            {
                Parallel.ForEach(dataRange, options, rangePartititon =>
                {
                    if (!shouldStop)
                    {
                        CheckSequentialRange(new() { min = rangePartititon.Item1, max = rangePartititon.Item2 });
                    }
                });
            },
            () =>
            {
                while (!shouldStop && !stopRange)
                {
                    if (CheckRandomRange(ranges, stopRange))
                    {
                        stopRange = true;
                    }
                }
            },
            () =>
            {
                while (!shouldStop && !stopRange)
                {
                    if (CheckRandomRange(ranges, stopRange))
                    {
                        stopRange = true;
                    }
                }
            },
            () =>
            {
                while (!shouldStop && !stopRange)
                {
                    if (CheckRandomRange(ranges, stopRange))
                    {
                        stopRange = true;
                    }
                }
            },
            () =>
            {
                while (!shouldStop && !stopRange)
                {
                    if (CheckRandomRange(ranges, stopRange))
                    {
                        stopRange = true;
                    }
                }
            },
            () =>
            {
                while (!shouldStop && !stopRange)
                {
                    if (CheckRandomRange(ranges, stopRange))
                    {
                        stopRange = true;
                    }
                }
            }
        );

        stopwatch.Stop();
        Console.WriteLine($"Total execution time: {stopwatch.Elapsed}");
    }

    private static string Green(string text) => $"\x1b[32m{text}\x1b[0m";
}
