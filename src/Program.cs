using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

const string filePath = "./measurements.txt";

//Hamburg; 12.0
//Bulawayo; 8.9
//Palembang; 38.8
//St.John's;15.2
//Cracow; 12.6
//Bridgetown; 26.9
//Istanbul; 6.2
//Roseau; 34.4
//Conakry; 31.2
//Istanbul; 23.0

//{ Abha = -23.0 / 18.0 / 59.2, Abidjan = -16.2 / 26.0 / 67.3, Abéché = -10.0 / 29.4 / 69.0, Accra = -10.1 / 26.4 / 66.4, Addis Ababa = -23.7 / 16.0 / 67.0, Adelaide = -27.8 / 17.3 / 58.5, ...}

HelpMe(filePath, 1024 * 256);

static void HelpMe(string path, int chunk)
{
    long start = Stopwatch.GetTimestamp();

    const byte Target = (byte)';';
    const byte Eol = (byte)'\n';
    const byte Negative = (byte)'-';

    int concurrent = Environment.ProcessorCount;

    FileStream fileStream = File.OpenRead(path);
    long blockSize = fileStream.Length / concurrent;
    fileStream.Close();

    Dictionary<int, Station>[] localMaps = new Dictionary<int, Station>[concurrent];
    for (int i = 0; i < localMaps.Length; i++)
    {
        localMaps[i] = new();
    }

    Parallel.For(0, concurrent, i =>
    {
        const ushort ChunkOffset = 32;
        int cacheLen = 0;

        using FileStream stream = File.OpenRead(filePath);
        
        Dictionary<int, Station> map = localMaps[i];

        Span<byte> chars = stackalloc byte[chunk];
        Span<byte> cache = stackalloc byte[chunk];

        long positionStart = i * blockSize;
        long targetLen = positionStart + blockSize;

        if (i != 0)
        {
            Span<byte> offsetBuffer = stackalloc byte[ChunkOffset];
            
            stream.Position = positionStart - ChunkOffset;
            stream.Read(offsetBuffer);

            int eolIndex = offsetBuffer.LastIndexOf(Eol);
            if (eolIndex != -1)
            {
                eolIndex = ChunkOffset - eolIndex - 1;
                positionStart -= eolIndex;

                if (i != concurrent - 1)
                {
                    targetLen += eolIndex;
                }
            }
        }

        stream.Position = positionStart;

        while (stream.Position < targetLen)
        {
            stream.Read(chars[cacheLen..]);

            if (cacheLen > 0)
            {
                cache[..cacheLen].CopyTo(chars);
                cache.Clear();
                cacheLen = 0;
            }

            Span<byte> localBuffer = chars;

            while (true)
            {
                int indexEnd = localBuffer.IndexOf(Eol);
                if (indexEnd == -1)
                {
                    cacheLen = localBuffer.Length;
                    localBuffer.CopyTo(cache);
                    chars.Clear();
                    break;
                }

                int indexTarget = localBuffer.IndexOf(Target) + 1;

                Span<byte> nameRaw = localBuffer[..indexTarget];
                Span<byte> valueRaw = localBuffer[indexTarget..indexEnd];
                localBuffer = localBuffer[(indexEnd + 1)..];

                bool negativeFlag = valueRaw[0] == Negative;

                if (negativeFlag)
                {
                    valueRaw = valueRaw[1..];
                }

                int value;

                if (valueRaw.Length == 4)
                {
                    value = (valueRaw[0] - '0') * 100 + (valueRaw[1] - '0') * 10 + (valueRaw[3] - '0');
                }
                else // valueRaw.Length == 3
                {
                    value = (valueRaw[0] - '0') * 10 + (valueRaw[2] - '0');
                }

                if (negativeFlag)
                {
                    value = -value;
                }

                int name;

                if (nameRaw.Length > 2)
                {
                    name = BitConverter.ToInt32(nameRaw);
                }
                else if (nameRaw.Length == 2)
                {
                    name = nameRaw[0] + nameRaw[1];
                }
                else
                {
                    name = nameRaw[0];
                }

                ref Station station = ref CollectionsMarshal.GetValueRefOrAddDefault(map, name, out bool exist);

                if (exist)
                {
                    station.Append(value);
                }
                else
                {
                    map[name] = new(nameRaw, value);
                }
            }
        }
    });

    Console.OutputEncoding = Encoding.UTF8;
    Console.Write('{');

    foreach (IGrouping<string, Station> stations in localMaps
        .SelectMany(s => s.Values)
        .GroupBy(b => b.Name)
        .OrderBy(c => c.Key, StringComparer.Ordinal))
    {
        Station station = stations.First();

        foreach (Station item in stations.Skip(1))
        {
            station.Attach(item);
        }

        Console.Write(station.Print());
    }

    Console.WriteLine($"}}\nCompleted in {Stopwatch.GetElapsedTime(start)}");
}

public struct Station
{
    private int _count;
    private int _total;

    private int _max;
    private int _min;

    private readonly string _name;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Station(Span<byte> name, int init)
    {
        _name = Encoding.UTF8.GetString(name[..^1]);

        Interlocked.Exchange(ref _total, init);
        Interlocked.Exchange(ref _max, init);
        Interlocked.Exchange(ref _min, init);
        Interlocked.Exchange(ref _count, 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Attach(Station station)
    {
        _count += station._count;
        _total += station._total;

        if (station._max > _max)
        {
            Interlocked.Exchange(ref _max, station._max);
        }
        else if (station._min < _min)
        {
            Interlocked.Exchange(ref _min, station._min);
        }
    }

    public readonly string Name => _name;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly string Print()
    {
        return $"{_name} = {_min / 10f}/{Math.Round(_total / 10f / _count , 1)}/{_max / 10f} ";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(int value)
    {
        _count++;
        _total += value;

        if (value > _max)
        {
            Interlocked.Exchange(ref _max, value);
        }
        else if (value < _min)
        {
            Interlocked.Exchange(ref _min, value);
        }
    }
}