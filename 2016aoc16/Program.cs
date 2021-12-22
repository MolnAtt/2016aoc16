using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2016aoc16
{
    static class Kiterjesztések
    {
        static Dictionary<char, string> hexszótár = new Dictionary<char, string>
            {
                {'0', "0000"},
                {'1', "0001"},
                {'2', "0010"},
                {'3', "0011"},
                {'4', "0100"},
                {'5', "0101"},
                {'6', "0110"},
                {'7', "0111"},
                {'8', "1000"},
                {'9', "1001"},
                {'A', "1010"},
                {'B', "1011"},
                {'C', "1100"},
                {'D', "1101"},
                {'E', "1110"},
                {'F', "1111"},
            };
        static string ToBin(char c) => hexszótár[c];
        public static string ToBin(this string s) => String.Join("", s.Select(ToBin));
        public static int ToInt(this string s) => Convert.ToInt32(s, 2);
        public static ulong ToULong(this string s) => Convert.ToUInt64(s, 2);
    }
    class Program
    {
            
        abstract class Packet
        {
            readonly protected int verzió;
            readonly protected int típusazonosító;
            public Packet(string bin)
            {
                verzió = bin.Substring(0, 3).ToInt();
                típusazonosító = típusazonosítója(bin);
            }

            static int típusazonosítója(string bin) => bin.Substring(3, 3).ToInt();
            public abstract int Verzióösszeg();
            public abstract ulong Kiértékel();
            public static Packet HexParse(string hex, out string maradék) => BinParse(hex.ToBin(), out maradék);
            public static Packet BinParse(string bin, out string maradék)
            {
                int tID = típusazonosítója(bin);
                switch (tID)
                {
                    case 0:
                        return new Összeg(bin, out maradék);
                    case 1:
                        return new Szorzat(bin, out maradék);
                    case 2:
                        return new Minimum(bin, out maradék);
                    case 3:
                        return new Maximum(bin, out maradék);
                    case 4:
                        return new Literál(bin, out maradék);
                    case 5:
                        return new Nagyobb(bin, out maradék);
                    case 6:
                        return new Kisebb(bin, out maradék);
                    case 7:
                        return new Egyenlő(bin, out maradék);
                    default:
                        maradék = "";
                        return null;
                }
            }
            public static void Diagnosztika(string hex)
            {
                Console.WriteLine($"=== a {hex} hexadecimális kód parszolása: ===");
                string s;
                Packet p = HexParse(hex, out s);
                Console.WriteLine(p);
                Console.WriteLine($"--- Kiértékelve: {p.Kiértékel()} -- ");
                Console.WriteLine($"--- maradék: {s} -- ");
                Console.WriteLine($"--- verzióösszeg: {p.Verzióösszeg()} -- ");
                Console.WriteLine("===============================================");
            }
        }
        class Literál : Packet
        {
            ulong szám;
            public Literál(string bin, out string maradék) : base(bin)
            {
                string s = "";
                int i = 6;
                while (i < bin.Length && bin[i] == '1')
                {
                    Add(ref s, bin, i);
                    i += 5;
                }
                Add(ref s, bin, i);
                szám = s.ToULong();
                maradék = bin.Substring(i + 4 + 1);
            }
            void Add(ref string s, string bin, int i) => s += bin.Substring(i + 1, 4);
            public override string ToString() => $"{szám}[L{típusazonosító}v{verzió}]";
            public override int Verzióösszeg() => verzió;
            public override ulong Kiértékel() => szám;

        }
        abstract class Művelet : Packet
        {
            readonly char lengthtypeID;
            int operandusok_összhossza;
            protected List<Packet> operandusok;
            public Művelet(string bin, out string maradék) : base(bin)
            {
                lengthtypeID = bin[6];
                operandusok = new List<Packet>();
                if (lengthtypeID == '0')
                {
                    operandusok_összhossza = bin.Substring(7, 15).ToInt();
                    string b = bin.Substring(7 + 15, operandusok_összhossza);
                    while (0 < b.Length)
                        operandusok.Add(BinParse(b, out b));
                    maradék = bin.Substring(7 + 15 + operandusok_összhossza);
                }
                else
                {
                    int operandusok_száma = bin.Substring(7, 11).ToInt();
                    string b = bin.Substring(7 + 11);
                    for (int i = 0; i < operandusok_száma; i++)
                        operandusok.Add(BinParse(b, out b));
                    maradék = b;
                }
            }
            protected string ToStringMaradék() => $"{típusazonosító}v{verzió}({String.Join(", ", operandusok.Select(x => x.ToString()))})";
            public override string ToString() => $"[O]{ToStringMaradék()}";
            public override int Verzióösszeg() => verzió + operandusok.Sum(x => x.Verzióösszeg());
            public ulong Összehasonlít(Func<ulong, ulong, bool> F) => F(operandusok[0].Kiértékel(), operandusok[1].Kiértékel()) ? (ulong)1 : 0;
        }
        class Összeg : Művelet
        {
            public Összeg(string bin, out string maradék) : base(bin, out maradék) { }
            public override ulong Kiértékel()
            {
                ulong sum = 0;
                foreach (Packet p in operandusok)
                    sum += p.Kiértékel();
                return sum;
            }
            public override string ToString() => $"[ÖSSZEG]{ToStringMaradék()}";
        }
        class Szorzat : Művelet
        {
            public Szorzat(string bin, out string maradék) : base(bin, out maradék) { }
            public override ulong Kiértékel()
            {
                ulong prod = 1;
                foreach (Packet p in operandusok)
                    prod *= p.Kiértékel();
                return prod;
            }
            public override string ToString() => $"[SZORZAT]{ToStringMaradék()}";
        }
        class Minimum : Művelet
        {
            public Minimum(string bin, out string maradék) : base(bin, out maradék) { }
            public override ulong Kiértékel()
            {
                ulong min = operandusok.First().Kiértékel();
                foreach (Packet p in operandusok.Skip(1))
                {
                    ulong temp = p.Kiértékel();
                    if (temp < min)
                        min = temp;
                }
                return min;
            }
            public override string ToString() => $"[MINIMUM]{ToStringMaradék()}";
        }
        class Maximum : Művelet
        {
            public Maximum(string bin, out string maradék) : base(bin, out maradék) { }
            public override ulong Kiértékel()
            {
                ulong max = operandusok.First().Kiértékel();
                foreach (Packet p in operandusok.Skip(1))
                {
                    ulong temp = p.Kiértékel();
                    if (max < temp)
                        max = temp;
                }
                return max;
            }
            public override string ToString() => $"[MAXIMUM]{ToStringMaradék()}";
        }
        class Nagyobb : Művelet
        {
            public Nagyobb(string bin, out string maradék) : base(bin, out maradék) { }
            public override ulong Kiértékel() => Összehasonlít((x, y) => x > y);
            public override string ToString() => $"[NAGYOBB]{ToStringMaradék()}";
        }
        class Kisebb : Művelet
        {
            public Kisebb(string bin, out string maradék) : base(bin, out maradék) { }
            public override ulong Kiértékel() => Összehasonlít((x, y) => x < y);
            public override string ToString() => $"[KISEBB]{ToStringMaradék()}";
        }

        class Egyenlő : Művelet
        {
            public Egyenlő(string bin, out string maradék) : base(bin, out maradék) { }
            public override ulong Kiértékel() => Összehasonlít((x, y) => x == y);
            public override string ToString() => $"[EGYENLŐ]{ToStringMaradék()}";
        }

        static void Main(string[] args)
        {
            Packet.Diagnosztika("D2FE28");
            Packet.Diagnosztika("38006F45291200");
            Packet.Diagnosztika("EE00D40C823060");
            Packet.Diagnosztika("8A004A801A8002F478");
            Packet.Diagnosztika("620080001611562C8802118E34");
            Packet.Diagnosztika("C0015000016115A2E0802F182340");
            Packet.Diagnosztika("A0016C880162017C3686B18A3D4780");
            Packet.Diagnosztika("C200B40A82");
            Packet.Diagnosztika("04005AC33890");
            Packet.Diagnosztika("880086C3E88112");
            Packet.Diagnosztika("CE00C43D881120");
            Packet.Diagnosztika("D8005AC2A8F0");
            Packet.Diagnosztika("F600BC2D8F");
            Packet.Diagnosztika("9C005AC2F8F0");
            Packet.Diagnosztika("9C0141080250320F1802104A08");
            Packet.Diagnosztika(System.IO.File.ReadAllText("input.txt"));
            Console.ReadLine();
        }
    }
}
