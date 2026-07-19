// ============================================================================
//  WarEngine.cs  —  موتور نبرد ترکیبی نسخه ۲ (Combined-Arms Battle Engine v2)
// ============================================================================
//  فایل مستقل؛ کنار Program.cs قرار می‌گیرد. هیچ تغییری در امضای عمومی لازم نیست.
//
//  نقاط ورود:
//    WarEngine.RunBattle(attacker, defender, tanks, soldiers, strategy, tactic)           ← سازگاری عقب‌رو
//    WarEngine.RunBattle(attacker, defender, tanks, soldiers, fighters, bombers,
//                        strategy, tactic, airStrategy, airTactic)                         ← نبرد ترکیبی
//    WarEngine.RunBattlesParallel(orders)                                                  ← اجرای موازی انبوه
// ============================================================================

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

// ─────────────────────────────────────────────────────────────────────────────
//  خروجی نبرد
// ─────────────────────────────────────────────────────────────────────────────
class BattleResult
{
    public long AttackerTanksLost;
    public long AttackerSoldiersLost;
    public long AttackerFightersLost;
    public long AttackerBombersLost;
    public long AttackerMoneyGained;
    public long AttackerIronGained;
    public double AttackerWelfareChange;
    public long DefenderTanksLost;
    public long DefenderSoldiersLost;
    public long DefenderFightersLost;
    public long DefenderAntiAirLost;
    public long DefenderMoneyLost;
    public long DefenderIronLost;
    public double DefenderWelfareChange;
    public string AttackerReport = "";
    public string DefenderReport = "";
    public string GroupAnnouncement = "";
    public double PenetrationKm;
    public int SuccessPercent;
    public bool AttackerWon;
    public bool AttackerFailed;
    public int DurationMinutes;
    public double AirSuperiority;
}

static class WarEngine
{
    // ───────────────────────────── ثوابت میدان ─────────────────────────────
    const float FRONT_KM = 40f;
    const float DEPTH_KM = 34f;
    const float WIN_DEPTH = 30f;
    const float FAIL_DEPTH = 3f;
    const int   GRID_W = 80, GRID_H = 68;
    const float CELL = 0.5f;
    const float TICK_MIN = 6f;
    const int   MAX_TICKS = 360;
    const int   AI_PERIOD = 4;
    const int   MAX_GROUPS = 224;
    const int   INF_GROUP = 100;
    const int   TANK_GROUP = 10;
    // انواع زمین
    const byte T_PLAIN = 0, T_HILL = 1, T_FOREST = 2, T_URBAN = 3, T_MARSH = 4, T_RIDGE = 5;
    static readonly float[] TerSpeed  = { 1.00f, 0.72f, 0.55f, 0.60f, 0.40f, 0.65f };
    static readonly float[] TerCover  = { 0.00f, 0.25f, 0.55f, 0.65f, 0.15f, 0.35f };
    static readonly float[] TerAcc    = { 1.00f, 0.90f, 0.70f, 0.65f, 0.95f, 0.92f };
    static readonly float[] TerVision = { 1.00f, 1.35f, 0.55f, 0.60f, 1.00f, 1.50f };
    // وضعیت‌های گروه
    const byte P_ADVANCE = 0, P_ASSAULT = 1, P_DEFEND = 2, P_AMBUSH = 3,
               P_PATROL = 4, P_RETREAT = 5, P_FLANK = 6, P_HOLD = 7;
    // ───────────────────────── آب‌وهوا و زمان ───────────────────────────────
    const byte W_CLEAR = 0, W_CLOUD = 1, W_RAIN = 2, W_FOG = 3, W_SNOW = 4;
    static readonly string[] WeatherName = { "آفتابی", "ابری", "بارانی", "مه‌آلود", "برفی" };
    static readonly float[] WxVision = { 1.00f, 0.92f, 0.78f, 0.50f, 0.70f };
    static readonly float[] WxAcc    = { 1.00f, 0.96f, 0.88f, 0.75f, 0.85f };
    static readonly float[] WxSpeed  = { 1.00f, 0.97f, 0.82f, 0.90f, 0.70f };
    static readonly float[] WxAir    = { 1.00f, 0.85f, 0.65f, 0.40f, 0.60f };
    const byte TM_DAWN = 0, TM_DAY = 1, TM_DUSK = 2, TM_NIGHT = 3;
    static readonly string[] TimeName = { "سپیده‌دم", "روز", "غروب", "شب" };
    static readonly float[] TimeVision = { 0.80f, 1.00f, 0.75f, 0.45f };
    static readonly float[] TimeAir    = { 0.85f, 1.00f, 0.80f, 0.50f };

    // ───────────────────────────── مشخصات تانک‌ها ───────────────────────────
    readonly struct TankSpec
    {
        public readonly string Name;
        public readonly float Pen, He, Mg, Armor, Speed, CannonAmmo, MgAmmo, Reliab;
        public TankSpec(string n, float p, float he, float mg, float ar, float sp, float ca, float ma, float rel)
        { Name = n; Pen = p; He = he; Mg = mg; Armor = ar; Speed = sp; CannonAmmo = ca; MgAmmo = ma; Reliab = rel; }
    }
    static readonly TankSpec SpecUSA   = new("M2 Medium", 46f, 0.45f, 7f, 30f, 42f, 100f, 90f, 0.95f);
    static readonly TankSpec SpecUSSR  = new("T-28",      40f, 1.00f, 4f, 80f, 37f,  70f, 60f, 0.82f);
    static readonly TankSpec SpecReich = new("Panzer III",67f, 0.55f, 3f, 60f, 40f,  84f, 55f, 0.97f);
    static TankSpec SpecOf(Faction f) => f == Faction.USA ? SpecUSA : f == Faction.USSR ? SpecUSSR : SpecReich;

    // ───────────────────────────── مشخصات هواپیماها ─────────────────────────
    readonly struct FighterSpec
    {
        public readonly string Name;
        public readonly float Maneuver, Firepower, Speed, Cas;
        public FighterSpec(string n, float mn, float fp, float sp, float cas)
        { Name = n; Maneuver = mn; Firepower = fp; Speed = sp; Cas = cas; }
    }
    static readonly FighterSpec FighterUSA   = new("P-36",   9f, 4.5f, 500f, 0.9f);
    static readonly FighterSpec FighterUSSR  = new("I-16",   9f, 4.0f, 520f, 0.8f);
    static readonly FighterSpec FighterReich = new("Bf 109", 8f, 8.0f, 570f, 1.0f);
    static FighterSpec FighterOf(Faction f) => f == Faction.USA ? FighterUSA : f == Faction.USSR ? FighterUSSR : FighterReich;

    readonly struct BomberSpec
    {
        public readonly string Name;
        public readonly float Armor, DefMg, Bombload, Speed;
        public BomberSpec(string n, float ar, float dmg, float bl, float sp)
        { Name = n; Armor = ar; DefMg = dmg; Bombload = bl; Speed = sp; }
    }
    static readonly BomberSpec BomberUSA   = new("B-17",   8f, 6f, 3600f, 460f);
    static readonly BomberSpec BomberReich = new("He 111", 5f, 4f, 2000f, 435f);
    static readonly BomberSpec BomberUSSR  = new("DB-3",   3f, 3f, 1000f, 430f);
    static BomberSpec BomberOf(Faction f) => f == Faction.USA ? BomberUSA : f == Faction.USSR ? BomberUSSR : BomberReich;

    static float FactionQuality(Faction f) => f switch
    {
        Faction.Reich => 1.08f,
        Faction.USA   => 1.03f,
        _             => 1.00f,
    };

    // ───────────────────────────── RNG سبک و قطعی ───────────────────────────
    struct XorRng
    {
        ulong s0, s1;
        public XorRng(ulong seed)
        {
            s0 = seed * 0x9E3779B97F4A7C15UL + 1; s1 = seed ^ 0xBF58476D1CE4E5B9UL;
            if (s1 == 0) s1 = 0x94D049BB133111EBUL;
            NextU(); NextU();
        }
        public ulong NextU()
        {
            ulong x = s0, y = s1; s0 = y;
            x ^= x << 23; s1 = x ^ y ^ (x >> 17) ^ (y >> 26);
            return s1 + y;
        }
        public float NextF() => (NextU() >> 40) * (1f / 16777216f);
        public float Range(float a, float b) => a + NextF() * (b - a);
        public int Next(int max) => (int)(NextU() % (uint)max);
    }

    // ───────────────────────────── ساختار گروه رزمی ─────────────────────────
    struct Group
    {
        public float X, Y;
        public float Units, Size0;
        public float CAmmo, MAmmo;
        public float Morale, Supp;
        public float Fatigue;
        public float Exp;
        public float TgtX, TgtY;
        public short FireTgt;
        public byte Type;
        public byte Posture;
        public byte Sector;
        public bool Alive;
        public bool Sprung;
        public float Signature;
    }
    struct Intel { public float Level, LastX, LastY, Stale; }
    struct Evt { public short Tick; public byte Kind; public float A, B; }

    const byte E_CONTACT = 0, E_AMBUSH = 1, E_BREAK5 = 2, E_BREAK10 = 3, E_BREAK20 = 4,
               E_BREAK30 = 5, E_AMMO = 6, E_ENCIRCLE = 7, E_ROUT = 8, E_DUEL = 9,
               E_SHIFT = 10, E_HALT = 11, E_SUPPLY = 12;

    // ───────────────────────── بافرهای ThreadStatic ─────────────────────────
    [ThreadStatic] static Group[] _atk;
    [ThreadStatic] static Group[] _def;
    [ThreadStatic] static Intel[] _intelA;
    [ThreadStatic] static Intel[] _intelD;
    [ThreadStatic] static byte[]  _terr;
    [ThreadStatic] static float[] _elev;
    [ThreadStatic] static float[] _threatA;
    [ThreadStatic] static float[] _threatD;
    [ThreadStatic] static Evt[]   _evts;
    [ThreadStatic] static StringBuilder _sb;

    static void EnsureBuffers()
    {
        _atk     ??= new Group[MAX_GROUPS];
        _def     ??= new Group[MAX_GROUPS];
        _intelA  ??= new Intel[MAX_GROUPS];
        _intelD  ??= new Intel[MAX_GROUPS];
        _terr    ??= new byte[GRID_W * GRID_H];
        _elev    ??= new float[GRID_W * GRID_H];
        _threatA ??= new float[10];
        _threatD ??= new float[10];
        _evts    ??= new Evt[96];
        _sb      ??= new StringBuilder(4096);
    }

    static long _seedCounter = Environment.TickCount;
    [ThreadStatic] static byte _weather;
    [ThreadStatic] static byte _startTime;

    static byte TimeAtTick(int tick)
    {
        int phase = (_startTime + (tick / 30)) & 3;
        return (byte)phase;
    }

    struct AirOutcome
    {
        public long AtkFightersLost, AtkBombersLost;
        public long DefFightersLost, DefAntiAirLost;
        public float Superiority;
        public float CasAtk, CasDef;
        public long StratMoney, StratIron;
        public float StratWelfare;
        public bool HadAirCombat, AtkHadAir, DefHadAir;
    }

    // ═════════════════════════════ API عمومی ════════════════════════════════
    public static BattleResult RunBattle(Country attacker, Country defender,
        long tanks, long soldiers, int strategy, int tactic)
        => RunBattle(attacker, defender, tanks, soldiers, 0, 0, strategy, tactic, 0, 0);

    public static BattleResult RunBattle(Country attacker, Country defender,
        long tanks, long soldiers, long fighters, long bombers,
        int strategy, int tactic, int airStrategy, int airTactic)
    {
        ulong seed = (ulong)Interlocked.Increment(ref _seedCounter)
                   ^ ((ulong)attacker.OwnerId << 20) ^ (ulong)DateTime.UtcNow.Ticks;
        return RunBattleSeeded(attacker, defender, tanks, soldiers, fighters, bombers,
                               strategy, tactic, airStrategy, airTactic, seed);
    }

    public struct BattleOrder
    {
        public Country Attacker, Defender;
        public long Tanks, Soldiers, Fighters, Bombers;
        public int Strategy, Tactic, AirStrategy, AirTactic;
    }

    public static BattleResult[] RunBattlesParallel(BattleOrder[] orders)
    {
        var results = new BattleResult[orders.Length];
        Parallel.For(0, orders.Length, i =>
        {
            var o = orders[i];
            results[i] = RunBattle(o.Attacker, o.Defender, o.Tanks, o.Soldiers, o.Fighters, o.Bombers,
                                   o.Strategy, o.Tactic, o.AirStrategy, o.AirTactic);
        });
        return results;
    }

    // ═════════════════════════ هسته شبیه‌سازی نبرد ═══════════════════════════
    public static BattleResult RunBattleSeeded(Country attacker, Country defender,
        long reqTanks, long reqSoldiers, long reqFighters, long reqBombers,
        int strategy, int tactic, int airStrategy, int airTactic, ulong seed)
    {
        EnsureBuffers();
        var rng = new XorRng(seed);
        var res = new BattleResult();

        long aTanks = Math.Max(0, Math.Min(reqTanks, attacker.Tanks));
        long aSold  = Math.Max(0, Math.Min(reqSoldiers, attacker.Soldiers));
        long aFight = Math.Max(0, Math.Min(reqFighters, attacker.Planes));
        long aBomb  = Math.Max(0, Math.Min(reqBombers, attacker.Bombers));
        long dTanks = Math.Min(defender.Tanks, Math.Max(defender.DefenseTanks, (long)Math.Ceiling(defender.Tanks * 0.2)));
        long dSold  = Math.Min(defender.Soldiers, Math.Max(defender.DefenseSoldiers, (long)Math.Ceiling(defender.Soldiers * 0.2)));
        long dFight = Math.Min(defender.Planes, defender.DefenseFighters);
        long dAA    = defender.AntiAir;

        int aStrat = strategy == 2 ? 2 : 1, aTac = tactic == 2 ? 2 : 1;
        int dStrat = defender.DefenseStrategy == 2 ? 2 : 1, dTac = defender.DefenseTactic == 2 ? 2 : 1;
        int aAirStrat = airStrategy == 2 ? 2 : (airStrategy == 1 ? 1 : 0);
        int aAirTac = airTactic == 2 ? 2 : 1;
        int dAirStrat = defender.AirDefStrategy == 2 ? 2 : 1;
        int dAirTac = defender.AirDefTactic == 2 ? 2 : 1;

        bool anyGround = (aTanks + aSold) > 0;
        bool anyAir = (aFight + aBomb) > 0;

        if (!anyGround && !anyAir)
        {
            res.AttackerReport = "⚠️ هیچ نیرویی اعزام نشد؛ حمله انجام نشد.";
            res.GroupAnnouncement = $"⚔️ حمله {attacker.Name} به {defender.Name} به دلیل نبود نیرو لغو شد.";
            res.AttackerFailed = true;
            return res;
        }

        var aSpec = SpecOf(attacker.Faction);
        var dSpec = SpecOf(defender.Faction);

        _weather = PickWeather(ref rng);
        _startTime = (byte)rng.Next(4);

        float counterAtk = StrategyCounter(aStrat, dStrat);

        AirOutcome air = RunAirPhase(attacker, defender, aFight, aBomb, aAirStrat, aAirTac,
                                     dFight, dAA, dStrat, dTac, dAirStrat, dAirTac, ref rng);

        res.AttackerFightersLost = air.AtkFightersLost;
        res.AttackerBombersLost = air.AtkBombersLost;
        res.DefenderFightersLost = air.DefFightersLost;
        res.DefenderAntiAirLost = air.DefAntiAirLost;
        res.AirSuperiority = Math.Round(air.Superiority, 2);

        long aTankLoss = 0, aSoldLoss = 0, dTankLoss = 0, dSoldLoss = 0;
        float effDepth = 0f, maxDepth = 0f;
        int tick = 0, evtN = 0;
        bool contact = false, ambushFired = false, encircled = false;
        int duelPeakTick = -1;
        float aIntelQ = 0f, dIntelQ = 0f;
        bool supplyStrain = false;
        bool defHasGround = (dTanks + dSold) > 0;

        if (!anyGround)
        {
            effDepth = 0f; maxDepth = 0f; tick = 30;
        }
        else if (!defHasGround)
        {
            GenTerrain(ref rng);
            int nA0 = BuildSide(_atk, true, aTanks, aSold, aStrat, aTac, ref rng);
            float airDrag = air.Superiority < -0.15f ? Math.Clamp(-air.Superiority, 0f, 1f) * 0.25f : 0f;
            float friction = 1f - airDrag;
            effDepth = WIN_DEPTH * friction; maxDepth = effDepth;
            CountLosses(_atk, nA0, ref aTankLoss, ref aSoldLoss);
            aTankLoss = (long)(aTankLoss * 0.02f);
            aSoldLoss = (long)(aSoldLoss * 0.02f);
            tick = 60;
        }
        else
        {
            GenTerrain(ref rng);
            int nA = BuildSide(_atk, true,  aTanks, aSold, aStrat, aTac, ref rng);
            int nD = BuildSide(_def, false, dTanks, dSold, dStrat, dTac, ref rng);
            for (int i = 0; i < nD; i++) { _intelA[i] = default; _intelA[i].Stale = 9999f; }
            for (int i = 0; i < nA; i++) { _intelD[i] = default; _intelD[i].Stale = 9999f; }

            float aQual = FactionQuality(attacker.Faction);
            float dQual = FactionQuality(defender.Faction);
            float aPow0 = SidePower(_atk, nA, aSpec), dPow0 = SidePower(_def, nD, dSpec);
            float prevMomentum = 0f; int haltTicks = 0; float duelPeak = 0f;
            float casA = air.CasAtk, casD = air.CasDef;

            if (air.Superiority > 0.05f)
                casA *= 1f + Math.Clamp(air.Superiority, 0f, 1f) * 0.45f;
            else if (air.Superiority < -0.05f)
                casD *= 1f + Math.Clamp(-air.Superiority, 0f, 1f) * 0.45f;

            if (air.Superiority > 0.25f) casD *= 1f - Math.Clamp(air.Superiority - 0.25f, 0f, 0.5f) * 0.4f;
            else if (air.Superiority < -0.25f) casA *= 1f - Math.Clamp(-air.Superiority - 0.25f, 0f, 0.5f) * 0.4f;

            if (defender.Cities <= 0) casD *= 1.5f;

            for (tick = 0; tick < MAX_TICKS; tick++)
            {
                byte tnow = TimeAtTick(tick);
                float visEnv = WxVision[_weather] * TimeVision[tnow];
                float accEnv = WxAcc[_weather];
                aIntelQ = SenseSide(_atk, nA, _def, nD, _intelA, aTac == 2 && aStrat == 1, visEnv, ref rng);
                dIntelQ = SenseSide(_def, nD, _atk, nA, _intelD, dStrat == 2, visEnv, ref rng);

                if (tick % AI_PERIOD == 0)
                {
                    BuildThreatMap(_def, nD, _intelA, nD, _threatA, dSpec);
                    BuildThreatMap(_atk, nA, _intelD, nA, _threatD, aSpec);
                    CommandAttacker(nA, nD, aStrat, aTac, effDepth, aIntelQ, ref rng, ref encircled, tick);
                    CommandDefender(nD, nA, dStrat, dTac, effDepth, dIntelQ, ref rng);
                }

                MoveSide(_atk, nA, aSpec, true, ref rng);
                MoveSide(_def, nD, dSpec, false, ref rng);

                float supplyA = SupplyFactor(effDepth);
                if (!supplyStrain && supplyA < 0.8f) { supplyStrain = true; AddEvt(ref evtN, tick, E_SUPPLY, effDepth); }

                float aDuel = FireSide(_atk, nA, aSpec, _def, nD, dSpec, _intelA, _intelD, true,  aStrat, aTac, dStrat, encircled, casA * counterAtk * aQual * supplyA, accEnv, ref rng, ref evtN, tick, ref contact, ref ambushFired);
                float dDuel = FireSide(_def, nD, dSpec, _atk, nA, aSpec, _intelD, _intelA, false, dStrat, dTac, aStrat, false,     casD * dQual, accEnv, ref rng, ref evtN, tick, ref contact, ref ambushFired);

                float duel = aDuel + dDuel;
                if (duel > duelPeak) { duelPeak = duel; duelPeakTick = tick; }

                MoraleSide(_atk, nA, true,  ref rng, ref evtN, tick);
                MoraleSide(_def, nD, false, ref rng, ref evtN, tick);

                float d = EffectiveDepth(_atk, nA);
                if (d > effDepth)
                {
                    float prev = effDepth; effDepth = d;
                    if (prev < 5f && d >= 5f) AddEvt(ref evtN, tick, E_BREAK5, d);
                    if (prev < 10f && d >= 10f) AddEvt(ref evtN, tick, E_BREAK10, d);
                    if (prev < 20f && d >= 20f) AddEvt(ref evtN, tick, E_BREAK20, d);
                    if (prev < 30f && d >= 30f) AddEvt(ref evtN, tick, E_BREAK30, d);
                    haltTicks = 0;
                }
                else haltTicks++;

                if (d > maxDepth) maxDepth = d;

                float aPow = SidePower(_atk, nA, aSpec), dPow = SidePower(_def, nD, dSpec);
                float momentum = (aPow / Math.Max(1f, aPow0)) - (dPow / Math.Max(1f, dPow0));
                if (tick > 20 && prevMomentum >= 0 && momentum < -0.12f) AddEvt(ref evtN, tick, E_SHIFT, 0);
                prevMomentum = momentum;

                if (effDepth >= WIN_DEPTH) { tick++; break; }
                if (aPow < aPow0 * 0.13f) { tick++; break; }
                if (dPow < dPow0 * 0.10f && effDepth > 6f)
                {
                    effDepth = Math.Min(WIN_DEPTH, effDepth + (WIN_DEPTH - effDepth) * 0.7f);
                    AddEvt(ref evtN, tick, E_ROUT, 1); tick++; break;
                }
                if (haltTicks > 90 && contact) { AddEvt(ref evtN, tick, E_HALT, effDepth); tick++; break; }
            }

            CountLosses(_atk, nA, ref aTankLoss, ref aSoldLoss);
            CountLosses(_def, nD, ref dTankLoss, ref dSoldLoss);

            if (effDepth >= 22f && effDepth < WIN_DEPTH)
                effDepth = Math.Min(WIN_DEPTH, effDepth + (WIN_DEPTH - effDepth) * 0.5f);
            else if (effDepth <= 6f && effDepth > FAIL_DEPTH)
                effDepth = Math.Max(0f, effDepth - (effDepth - FAIL_DEPTH) * 0.5f);
        }

        aTankLoss = Math.Min(aTankLoss, aTanks); aSoldLoss = Math.Min(aSoldLoss, aSold);
        dTankLoss = Math.Min(dTankLoss, dTanks); dSoldLoss = Math.Min(dSoldLoss, dSold);

        float frac = Math.Clamp((effDepth - FAIL_DEPTH) / (WIN_DEPTH - FAIL_DEPTH), 0f, 1f);
        int success = (int)Math.Round(frac * 100);
        bool absWin = effDepth >= WIN_DEPTH;
        bool absFail = anyGround && effDepth < FAIL_DEPTH;

        long lootMoney = (long)(defender.Money * 0.15 * frac);
        long lootIron  = (long)(defender.Iron  * 0.10 * frac);
        lootMoney = Math.Min(lootMoney, defender.Money);
        lootIron  = Math.Min(lootIron, defender.Iron);

        long stratMoney = Math.Min(air.StratMoney, Math.Max(0, defender.Money - lootMoney));
        long stratIron  = Math.Min(air.StratIron,  Math.Max(0, defender.Iron  - lootIron));

        res.AttackerTanksLost = aTankLoss;
        res.AttackerSoldiersLost = aSoldLoss;
        res.DefenderTanksLost = dTankLoss;
        res.DefenderSoldiersLost = dSoldLoss;
        res.AttackerMoneyGained = lootMoney;
        res.AttackerIronGained = lootIron;
        res.DefenderMoneyLost = lootMoney + stratMoney;
        res.DefenderIronLost = lootIron + stratIron;
        res.PenetrationKm = Math.Round(effDepth, 1);
        res.SuccessPercent = success;
        res.AttackerWon = absWin;
        res.AttackerFailed = absFail;
        res.DurationMinutes = Math.Max(30, (int)(tick * TICK_MIN));

        double aLossR = (aTanks + aSold) > 0 ? (aTankLoss * 10.0 + aSoldLoss) / Math.Max(1.0, aTanks * 10.0 + aSold) : 0;
        double dLossR = (dTanks + dSold) > 0 ? (dTankLoss * 10.0 + dSoldLoss) / Math.Max(1.0, dTanks * 10.0 + dSold) : 0;

        res.AttackerWelfareChange = -Math.Clamp(aLossR * 2.0 + (res.AttackerFailed ? 1.0 : 0), 0, 3);
        res.DefenderWelfareChange = -Math.Clamp(dLossR * 2.0 + (absWin ? 1.5 : 0) + frac * 0.8 + air.StratWelfare * 0.3, 0, 4);

        BuildReports(res, attacker, defender, aSpec, dSpec, aStrat, aTac, dStrat, dTac,
            aTanks, aSold, dTanks, dSold, aFight, aBomb, dFight, dAA,
            aAirStrat, aAirTac, dAirStrat, dAirTac, air,
            evtN, duelPeakTick, encircled, ambushFired, aIntelQ, dIntelQ, effDepth, frac,
            anyGround, defHasGround, supplyStrain, counterAtk);

        SaveBattle(attacker, defender, res);
        return res;
    }

    static float StrategyCounter(int aStrat, int dStrat)
    {
        if (aStrat == 1 && dStrat == 1) return 1.08f;
        if (aStrat == 1 && dStrat == 2) return 0.92f;
        if (aStrat == 2 && dStrat == 1) return 1.12f;
        return 1.00f;
    }

    static byte PickWeather(ref XorRng rng)
    {
        float r = rng.NextF();
        if (r < 0.45f) return W_CLEAR;
        if (r < 0.68f) return W_CLOUD;
        if (r < 0.84f) return W_RAIN;
        if (r < 0.94f) return W_FOG;
        return W_SNOW;
    }

    static float SupplyFactor(float depth)
    {
        if (depth <= 10f) return 1f;
        return Math.Clamp(1f - (depth - 10f) / 50f, 0.6f, 1f);
    }

    static void AddEvt(ref int n, int tick, byte kind, float a, float b = 0)
    {
        if (n >= _evts.Length) return;
        _evts[n].Tick = (short)tick; _evts[n].Kind = kind; _evts[n].A = a; _evts[n].B = b;
        n++;
    }

    // ═════════════════════════ فاز هوایی ════════════════════════════════════
    static AirOutcome RunAirPhase(Country atk, Country def,
        long aFight, long aBomb, int aAirStrat, int aAirTac,
        long dFight, long dAA, int dStrat, int dTac, int dAirStrat, int dAirTac, ref XorRng rng)
    {
        var o = new AirOutcome { CasAtk = 1f, CasDef = 1f };
        o.AtkHadAir = (aFight + aBomb) > 0;
        o.DefHadAir = (dFight + dAA) > 0;
        if (!o.AtkHadAir && !o.DefHadAir) { o.Superiority = 0f; return o; }

        var aFs = FighterOf(atk.Faction);
        var aBs = BomberOf(atk.Faction);
        var dFs = FighterOf(def.Faction);

        float wxAir = WxAir[_weather];
        float aFighterQ = (aFs.Maneuver * 0.55f + aFs.Firepower * 0.45f) * FactionQuality(atk.Faction);
        float dFighterQ = (dFs.Maneuver * 0.55f + dFs.Firepower * 0.45f) * FactionQuality(def.Faction);
        float capBonus = (dAirStrat == 1 && dAirTac == 1) ? 1.25f : 1f;
        float flakBonus = (dAirStrat == 2 && dAirTac == 1) ? 1.35f : 1f;
        if (dAirStrat == 2 && dAirTac == 2) capBonus *= 1.1f;

        float aFighterPow = aFight * aFighterQ * wxAir * rng.Range(0.9f, 1.1f);
        float dFighterPow = dFight * dFighterQ * capBonus * rng.Range(0.9f, 1.1f);

        long aFightLost = 0, dFightLost = 0;
        if (aFight > 0 && dFight > 0)
        {
            o.HadAirCombat = true;
            float total = aFighterPow + dFighterPow;
            float aLossFrac = Math.Clamp(dFighterPow / Math.Max(1f, total) * rng.Range(0.7f, 1.1f), 0f, 0.95f);
            float dLossFrac = Math.Clamp(aFighterPow / Math.Max(1f, total) * rng.Range(0.7f, 1.1f), 0f, 0.95f);
            aFightLost = (long)Math.Round(aFight * aLossFrac);
            dFightLost = (long)Math.Round(dFight * dLossFrac);
        }
        long aBombLost = 0, dAALost = 0;
        if (aBomb > 0 && dFight > 0 && aFight == 0)
        {
            o.HadAirCombat = true;
            float interceptPower = dFight * dFighterQ * capBonus * rng.Range(0.8f, 1.1f);
            long intercepted = (long)Math.Round(Math.Min(aBomb, interceptPower * 0.015f / (1f + aBs.Armor * 0.3f)));
            aBombLost += Math.Min(aBomb, intercepted);
        }

        long aFightLeft = aFight - aFightLost;
        long dFightLeft = dFight - dFightLost;

        if (dAA > 0 && (aFightLeft > 0 || aBomb > 0))
        {
            float aaPower = dAA * flakBonus * rng.Range(0.85f, 1.15f);
            float bomberResist = 1f / (1f + aBs.Armor * 0.25f);
            long bombHit = (long)Math.Round(Math.Min(Math.Max(0, aBomb - aBombLost), aaPower * 0.015f * bomberResist));
            aBombLost = Math.Min(aBomb, aBombLost + bombHit);

            long fightHit = (long)Math.Round(Math.Min(aFightLeft, aaPower * 0.02f));
            aFightLost += fightHit; aFightLeft -= fightHit;

            float incoming = aFightLeft + (aBomb - aBombLost) * 1.3f;
            dAALost = (long)Math.Round(Math.Min(dAA, incoming * rng.Range(0.03f, 0.07f)));
        }

        long aBombLeft = aBomb - aBombLost;
        float atkAirRemain = aFightLeft * aFighterQ + aBombLeft * 1.0f;
        float defAirRemain = dFightLeft * dFighterQ + dAA * 0.5f;
        float sup = (atkAirRemain - defAirRemain) / Math.Max(1f, atkAirRemain + defAirRemain);
        o.Superiority = Math.Clamp(sup, -1f, 1f);

        if (aAirStrat == 1)
        {
            if (aAirTac == 2 && aBombLeft > 0 && dFightLeft > 0)
            {
                float raidIntensity = aBombLeft * (aBs.Bombload / 3600f) * wxAir * (0.5f + 0.5f * Math.Clamp(o.Superiority + 0.5f, 0f, 1f));
                long grounded = (long)Math.Round(Math.Min(dFightLeft, raidIntensity * rng.Range(0.6f, 1.0f)));
                if (grounded > 0) { dFightLost += grounded; dFightLeft -= grounded; }
            }
            float casPower = (aFightLeft * aFs.Cas + aBombLeft * 1.5f) * wxAir;
            o.CasAtk = 1f + Math.Clamp(casPower / Math.Max(50f, (atk.Soldiers + 1) * 0.02f), 0f, 0.6f);
            if (o.Superiority < -0.1f)
                o.CasDef = 1f + Math.Clamp(dFightLeft * dFs.Cas / Math.Max(50f, (def.Soldiers + 1) * 0.02f), 0f, 0.4f);
        }
        else if (aAirStrat == 2)
        {
            float effBomb = aBombLeft * (0.55f + 0.45f * Math.Clamp(o.Superiority + 0.5f, 0f, 1f)) * wxAir;
            float perBomberDamage = aBs.Bombload / 3600f;
            float intensity = effBomb * perBomberDamage;
            float moneyFrac = Math.Clamp(intensity * 0.02f, 0f, aAirTac == 1 ? 0.35f : 0.30f);
            float ironFrac  = Math.Clamp(intensity * 0.02f, 0f, aAirTac == 1 ? 0.40f : 0.18f);
            if (aAirTac == 1)
            {
                o.StratMoney = (long)(def.Money * moneyFrac * 0.9f);
                o.StratIron  = (long)(def.Iron * ironFrac);
                o.StratWelfare = Math.Clamp(effBomb * 0.02f, 0f, 4f);
            }
            else
            {
                o.StratMoney = (long)(def.Money * moneyFrac);
                o.StratIron  = (long)(def.Iron * ironFrac * 0.5f);
                o.StratWelfare = Math.Clamp(effBomb * 0.02f, 0f, 2f);
            }
            o.CasAtk = 1f + Math.Clamp(aFightLeft * aFs.Cas / Math.Max(80f, (atk.Soldiers + 1) * 0.03f), 0f, 0.3f);
        }

        o.AtkFightersLost = Math.Min(aFight, Math.Max(0, aFightLost));
        o.AtkBombersLost  = Math.Min(aBomb, Math.Max(0, aBombLost));
        o.DefFightersLost = Math.Min(dFight, Math.Max(0, dFightLost));
        o.DefAntiAirLost  = Math.Min(dAA, Math.Max(0, dAALost));

        return o;
    }

    // ═════════════════════════ تولید زمین ═══════════════════════════════════
    static float Hash(int x, int y, uint s)
    {
        uint h = (uint)(x * 374761393 + y * 668265263) ^ s;
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
    }

    static float Noise(float x, float y, uint s)
    {
        int xi = (int)MathF.Floor(x), yi = (int)MathF.Floor(y);
        float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        float a = Hash(xi, yi, s), b = Hash(xi + 1, yi, s), c = Hash(xi, yi + 1, s), d = Hash(xi + 1, yi + 1, s);
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }

    static void GenTerrain(ref XorRng rng)
    {
        uint s1 = (uint)rng.NextU(), s2 = (uint)rng.NextU(), s3 = (uint)rng.NextU();
        for (int gy = 0; gy < GRID_H; gy++)
        for (int gx = 0; gx < GRID_W; gx++)
        {
            float e = Noise(gx * 0.09f, gy * 0.09f, s1) * 0.65f + Noise(gx * 0.23f, gy * 0.23f, s2) * 0.35f;
            float v = Noise(gx * 0.13f + 50, gy * 0.13f, s3);
            int idx = gy * GRID_W + gx;
            _elev[idx] = e;
            byte t;
            if (e > 0.78f) t = T_RIDGE;
            else if (e > 0.62f) t = T_HILL;
            else if (v > 0.72f && e > 0.3f) t = T_FOREST;
            else if (v < 0.12f && e < 0.35f) t = T_MARSH;
            else if (v > 0.62f && v <= 0.72f && e < 0.5f) t = T_URBAN;
            else t = T_PLAIN;
            _terr[idx] = t;
        }
    }

    static byte TerrAt(float x, float y)
    {
        int gx = (int)(x / CELL); int gy = (int)((y + 6f) / CELL);
        if (gx < 0) gx = 0; if (gx >= GRID_W) gx = GRID_W - 1;
        if (gy < 0) gy = 0; if (gy >= GRID_H) gy = GRID_H - 1;
        return _terr[gy * GRID_W + gx];
    }

    static float ElevAt(float x, float y)
    {
        int gx = Math.Clamp((int)(x / CELL), 0, GRID_W - 1);
        int gy = Math.Clamp((int)((y + 6f) / CELL), 0, GRID_H - 1);
        return _elev[gy * GRID_W + gx];
    }

    // ═════════════════════════ ساخت گروه‌های یک طرف ═════════════════════════
    static int BuildSide(Group[] g, bool atk, long tanks, long soldiers, int strat, int tac, ref XorRng rng)
    {
        long rawGroups = tanks / TANK_GROUP + soldiers / INF_GROUP + 2;
        float scale = rawGroups > MAX_GROUPS ? (float)rawGroups / MAX_GROUPS : 1f;
        float tankGrp = TANK_GROUP * scale, infGrp = INF_GROUP * scale;
        int n = 0;
        long tLeft = tanks, sLeft = soldiers;
        while (tLeft > 0 && n < MAX_GROUPS)
        {
            float u = (float)Math.Min(tLeft, (long)Math.Ceiling(tankGrp));
            InitGroup(ref g[n], atk, 1, u, strat, tac, ref rng);
            tLeft -= (long)u; n++;
        }
        while (sLeft > 0 && n < MAX_GROUPS)
        {
            float u = (float)Math.Min(sLeft, (long)Math.Ceiling(infGrp));
            InitGroup(ref g[n], atk, 0, u, strat, tac, ref rng);
            sLeft -= (long)u; n++;
        }
        return n;
    }

    static void InitGroup(ref Group gr, bool atk, byte type, float units, int strat, int tac, ref XorRng rng)
    {
        gr = default;
        gr.Type = type; gr.Units = units; gr.Size0 = units; gr.Alive = true;
        gr.Morale = rng.Range(0.85f, 1f);
        gr.CAmmo = units; gr.MAmmo = units;
        gr.Fatigue = 0f; gr.Exp = rng.Range(0f, 0.1f);
        gr.FireTgt = -1;
        if (atk)
        {
            gr.Y = rng.Range(-4.5f, -1.5f);
            if (strat == 1)
            {
                float c = tac == 1 ? FRONT_KM * 0.5f : (rng.NextF() < 0.5f ? FRONT_KM * 0.3f : FRONT_KM * 0.7f);
                gr.X = Math.Clamp(c + rng.Range(-5f, 5f), 1f, FRONT_KM - 1);
            }
            else { gr.X = rng.Range(1f, FRONT_KM - 1); gr.Posture = P_FLANK; }
            gr.Posture = gr.Posture == P_FLANK ? P_FLANK : P_ADVANCE;
            gr.TgtX = gr.X; gr.TgtY = 8f;
        }
        else
        {
            gr.X = rng.Range(1f, FRONT_KM - 1);
            if (strat == 1)
            {
                gr.Y = tac == 1 ? rng.Range(0.8f, 3.2f) : rng.Range(1.5f, 6f);
                gr.Posture = tac == 1 ? P_DEFEND : P_PATROL;
                if (tac == 1) SeekCover(ref gr, ref rng);
            }
            else
            {
                gr.Y = tac == 1 ? rng.Range(2f, 7f) : rng.Range(4f, 10f);
                gr.Posture = P_AMBUSH;
                SeekCover(ref gr, ref rng);
            }
            gr.TgtX = gr.X; gr.TgtY = gr.Y;
        }
        gr.Sector = (byte)Math.Clamp((int)(gr.X / (FRONT_KM / 10f)), 0, 9);
    }

    static void SeekCover(ref Group gr, ref XorRng rng)
    {
        float bx = gr.X, by = gr.Y, best = TerCover[TerrAt(gr.X, gr.Y)];
        for (int i = 0; i < 6; i++)
        {
            float x = Math.Clamp(gr.X + rng.Range(-2f, 2f), 0.5f, FRONT_KM - 0.5f);
            float y = Math.Clamp(gr.Y + rng.Range(-1.5f, 1.5f), 0.3f, DEPTH_KM - 1);
            float c = TerCover[TerrAt(x, y)];
            if (c > best) { best = c; bx = x; by = y; }
        }
        gr.X = bx; gr.Y = by;
    }

    // ═════════════════ مه جنگ: شناسایی + اشتراک اطلاعات (با محیط) ════════════
    static float SenseSide(Group[] own, int nOwn, Group[] foe, int nFoe, Intel[] intel, bool reconBonus, float visEnv, ref XorRng rng)
    {
        float sum = 0f; int alive = 0;
        for (int j = 0; j < nFoe; j++)
        {
            if (!foe[j].Alive) { intel[j].Level *= 0.9f; continue; }
            alive++;
            ref Intel it = ref intel[j];
            it.Stale += TICK_MIN;
            float bestGain = 0f;
            byte ft = TerrAt(foe[j].X, foe[j].Y);
            float conceal = TerCover[ft];
            if (foe[j].Posture == P_AMBUSH && !foe[j].Sprung) conceal = Math.Min(0.92f, conceal + 0.35f);
            float sig = foe[j].Signature;
            for (int i = 0; i < nOwn; i++)
            {
                if (!own[i].Alive) continue;
                float dx = own[i].X - foe[j].X, dy = own[i].Y - foe[j].Y;
                float dist2 = dx * dx + dy * dy;
                if (dist2 > 36f) continue;
                float dist = MathF.Sqrt(dist2);
                float vis = (own[i].Type == 1 ? 2.6f : 2.1f) * TerVision[TerrAt(own[i].X, own[i].Y)] * visEnv;
                if (ElevAt(own[i].X, own[i].Y) > ElevAt(foe[j].X, foe[j].Y) + 0.12f) vis *= 1.3f;
                if (reconBonus) vis *= 1.25f;
                float moveSig = foe[j].Posture is P_ADVANCE or P_FLANK or P_ASSAULT ? 0.25f : 0f;
                float p = (1f - Math.Clamp(dist / Math.Max(0.3f, vis), 0f, 1f)) * (1f - conceal) + sig + moveSig;
                if (p > bestGain) bestGain = p;
            }
            if (bestGain > 0.04f && rng.NextF() < Math.Clamp(bestGain, 0f, 0.95f))
            {
                it.Level = Math.Min(1f, it.Level + 0.45f + bestGain * 0.5f);
                it.LastX = foe[j].X; it.LastY = foe[j].Y; it.Stale = 0f;
            }
            else
            {
                it.Level *= it.Stale > 60f ? 0.93f : 0.985f;
                if (it.Stale > 150f) it.Level *= 0.85f;
            }
            sum += it.Level;
        }
        for (int j = 0; j < nFoe; j++) { ref var f = ref foe[j]; f.Signature *= 0.55f; }
        return alive > 0 ? sum / alive : 0f;
    }

    static void BuildThreatMap(Group[] foe, int nFoe, Intel[] intel, int nIntel, float[] map, TankSpec foeSpec)
    {
        Array.Clear(map, 0, 10);
        for (int j = 0; j < nFoe; j++)
        {
            if (!foe[j].Alive || intel[j].Level < 0.15f) continue;
            int s = Math.Clamp((int)(intel[j].LastX / (FRONT_KM / 10f)), 0, 9);
            float pw = foe[j].Type == 1 ? foe[j].Units * 9f : foe[j].Units * 0.8f;
            map[s] += pw * intel[j].Level;
        }
    }

    static int WeakestSector(float[] threat, ref XorRng rng)
    {
        int best = 0; float bv = float.MaxValue;
        for (int s = 1; s < 9; s++)
        {
            float v = threat[s] * 1f + threat[s - 1] * 0.4f + threat[s + 1] * 0.4f + rng.NextF() * 8f;
            if (v < bv) { bv = v; best = s; }
        }
        return best;
    }

    static void CommandAttacker(int nA, int nD, int strat, int tac, float depth, float intelQ,
        ref XorRng rng, ref bool encircled, int tick)
    {
        int weak = WeakestSector(_threatA, ref rng);
        float mainX = (weak + 0.5f) * (FRONT_KM / 10f);
        for (int i = 0; i < nA; i++)
        {
            ref Group g = ref _atk[i];
            if (!g.Alive || g.Posture == P_RETREAT) continue;
            float ammoR = (g.CAmmo + g.MAmmo) / Math.Max(0.01f, g.Size0 * 2f);
            if (ammoR <= 0.02f) { g.Posture = P_RETREAT; g.TgtY = -4f; continue; }
            if (ammoR < 0.18f) { g.Posture = P_HOLD; continue; }
            if (g.Morale < 0.35f) { g.Posture = P_HOLD; continue; }
            if (strat == 1)
            {
                bool probing = tac == 2 && tick < 40 && intelQ < 0.35f;
                g.Posture = probing ? P_PATROL : (depth > 2f ? P_ASSAULT : P_ADVANCE);
                float spread = probing ? 14f : (tac == 1 ? 4f : 7f);
                g.TgtX = Math.Clamp(mainX + rng.Range(-spread, spread), 1f, FRONT_KM - 1);
                g.TgtY = g.Y + 6f;
            }
            else
            {
                bool leftArm = (i & 1) == 0;
                float armX = leftArm ? mainX - 8f - depth * 0.3f : mainX + 8f + depth * 0.3f;
                if (tac == 2) armX += MathF.Sin((tick + i * 7) * 0.05f) * 5f;
                g.TgtX = Math.Clamp(armX + rng.Range(-3f, 3f), 1f, FRONT_KM - 1);
                g.TgtY = g.Y + (g.Type == 1 ? 6f : 4f);
                g.Posture = P_FLANK;
                if (depth > 8f && !encircled && intelQ > 0.45f) encircled = true;
            }
        }
    }

    static void CommandDefender(int nD, int nA, int strat, int tac, float depth, float intelQ, ref XorRng rng)
    {
        int hot = 0; float hv = -1f;
        for (int s = 0; s < 10; s++) if (_threatD[s] > hv) { hv = _threatD[s]; hot = s; }
        float hotX = (hot + 0.5f) * (FRONT_KM / 10f);
        for (int i = 0; i < nD; i++)
        {
            ref Group g = ref _def[i];
            if (!g.Alive || g.Posture == P_RETREAT) continue;
            float ammoR = (g.CAmmo + g.MAmmo) / Math.Max(0.01f, g.Size0 * 2f);
            if (ammoR <= 0.02f) { g.Posture = P_RETREAT; g.TgtY = Math.Min(DEPTH_KM - 1, g.Y + 6f); continue; }
            if (strat == 1)
            {
                if (tac == 1)
                {
                    bool reserve = i % 3 == 2;
                    if (reserve && hv > 0 && depth > 1f)
                    { g.TgtX = Math.Clamp(hotX + rng.Range(-3f, 3f), 1f, FRONT_KM - 1); g.TgtY = Math.Max(0.8f, depth - 1f); g.Posture = P_ADVANCE; }
                    else g.Posture = P_DEFEND;
                }
                else
                {
                    if (hv > 0) { g.TgtX = Math.Clamp(hotX + rng.Range(-5f, 5f), 1f, FRONT_KM - 1); g.TgtY = Math.Clamp(depth + rng.Range(0f, 2f), 1f, 8f); g.Posture = P_ADVANCE; }
                    else { g.TgtX = Math.Clamp(g.X + rng.Range(-6f, 6f), 1f, FRONT_KM - 1); g.Posture = P_PATROL; }
                }
            }
            else
            {
                if (tac == 1)
                {
                    if (!g.Sprung) { g.Posture = P_AMBUSH; continue; }
                    g.Posture = P_ASSAULT;
                    g.TgtX = Math.Clamp(hotX + rng.Range(-4f, 4f), 1f, FRONT_KM - 1);
                    g.TgtY = Math.Max(1f, depth);
                }
                else
                {
                    if (depth < 8f && !g.Sprung) { g.TgtY = Math.Min(12f, g.Y + 1.5f); g.Posture = P_AMBUSH; }
                    else { g.Posture = P_ASSAULT; g.TgtX = Math.Clamp(hotX + rng.Range(-6f, 6f), 1f, FRONT_KM - 1); g.TgtY = Math.Max(1f, depth - 2f); }
                }
            }
        }
    }

    static void MoveSide(Group[] g, int n, TankSpec spec, bool atk, ref XorRng rng)
    {
        float wxSpd = WxSpeed[_weather];
        for (int i = 0; i < n; i++)
        {
            ref Group u = ref g[i];
            if (!u.Alive) continue;
            if (u.Posture is P_DEFEND or P_AMBUSH or P_HOLD) continue;
            float baseKmH = u.Type == 1 ? spec.Speed * 0.32f : 4.2f;
            if (u.Posture == P_RETREAT) baseKmH *= 1.2f;
            if (u.Supp > 0.5f) baseKmH *= 0.45f;
            baseKmH *= (1f - u.Fatigue * 0.3f);
            float ter = TerSpeed[TerrAt(u.X, u.Y)];
            float step = baseKmH * ter * wxSpd * (TICK_MIN / 60f);
            float dx = u.TgtX - u.X, dy = u.TgtY - u.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 0.15f) continue;
            float mv = Math.Min(step, dist);
            u.X += dx / dist * mv; u.Y += dy / dist * mv;
            u.X = Math.Clamp(u.X, 0.2f, FRONT_KM - 0.2f);
            u.Y = Math.Clamp(u.Y, -6f, DEPTH_KM);
            if (mv > 0.5f) u.Signature = Math.Min(1f, u.Signature + 0.18f);
            u.Sector = (byte)Math.Clamp((int)(u.X / (FRONT_KM / 10f)), 0, 9);
        }
    }

    // ═════════════ آتش: زره/مسلسل/HE + پشتیبانی هوایی + محیط + تجربه ════════
    static float FireSide(Group[] own, int nOwn, TankSpec ospec, Group[] foe, int nFoe, TankSpec fspec,
        Intel[] ownIntel, Intel[] foeIntel, bool atk, int strat, int tac, int foeStrat, bool encircled,
        float combatMul, float accEnv, ref XorRng rng, ref int evtN, int tick, ref bool contact, ref bool ambushFired)
    {
        float duel = 0f;
        for (int i = 0; i < nOwn; i++)
        {
            ref Group u = ref own[i];
            if (!u.Alive || u.Posture == P_RETREAT) continue;
            int best = -1; float bestScore = 0f; float bestDist = 99f;
            float maxRange = u.Type == 1 ? 2.1f : 0.9f;
            for (int j = 0; j < nFoe; j++)
            {
                if (!foe[j].Alive) continue;
                float lvl = ownIntel[j].Level;
                if (lvl < 0.2f) continue;
                float dx = foe[j].X - u.X, dy = foe[j].Y - u.Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist > maxRange + 0.6f) continue;
                float pri = u.Type == 1 ? (foe[j].Type == 1 ? 3f : 1.6f) : (foe[j].Type == 1 ? 0.6f : 2.2f);
                pri *= 1f + (1f - foe[j].Units / Math.Max(1f, foe[j].Size0)) * 0.8f;
                float score = pri * lvl / (0.4f + dist);
                if (score > bestScore) { bestScore = score; best = j; bestDist = dist; }
            }
            u.FireTgt = (short)best;
            if (best < 0) continue;
            if (bestDist > maxRange) continue;
            if (!contact) { contact = true; AddEvt(ref evtN, tick, E_CONTACT, u.X); }
            float ambushMul = 1f;
            if (u.Posture == P_AMBUSH && !u.Sprung)
            {
                u.Sprung = true; ambushMul = 2.6f;
                if (!ambushFired) { ambushFired = true; AddEvt(ref evtN, tick, E_AMBUSH, u.X, u.Y); }
            }
            ref Group t = ref foe[best];
            float intelQ = ownIntel[best].Level;
            byte tt = TerrAt(t.X, t.Y);
            float acc = 0.62f * (0.45f + 0.55f * intelQ) * TerAcc[TerrAt(u.X, u.Y)] * accEnv * (1f - u.Supp * 0.5f);
            acc *= (0.9f + u.Exp * 0.3f);
            if (u.Posture is P_ADVANCE or P_ASSAULT or P_FLANK) acc *= 0.78f;
            if (ElevAt(u.X, u.Y) > ElevAt(t.X, t.Y) + 0.1f) acc *= 1.18f;
            float cover = TerCover[tt] * (t.Posture is P_DEFEND or P_AMBUSH or P_HOLD ? 1.25f : 0.8f);
            float ammoR = (u.CAmmo + u.MAmmo) / Math.Max(0.01f, u.Size0 * 2f);
            float ammoMul = ammoR > 0.5f ? 1f : 0.55f + ammoR * 0.9f;
            float encMul = atk && encircled && strat == 2 ? 1.25f : 1f;
            float morale = 0.55f + u.Morale * 0.45f;
            float k = acc * ammoMul * encMul * morale * ambushMul * combatMul * (1f - u.Fatigue * 0.25f) * (TICK_MIN / 6f);
            if (u.Type == 1)
            {
                float rangeMul = Math.Clamp(1.25f - bestDist * 0.45f, 0.45f, 1.2f);
                if (t.Type == 1)
                {
                    if (u.CAmmo > 0.05f)
                    {
                        float effArmor = fspec.Armor * (t.Posture is P_DEFEND or P_AMBUSH ? 1.3f : 1f);
                        float pen = 1f / (1f + MathF.Exp(-(ospec.Pen * rangeMul - effArmor) / 9f));
                        float shots = u.Units * 1.6f * k;
                        float kills = shots * 0.32f * pen * (0.9f + rng.NextF() * 0.25f);
                        ApplyDamage(ref t, kills, foeIntel, best);
                        u.CAmmo = Math.Max(0f, u.CAmmo - shots * 0.05f);
                        u.Signature = Math.Min(1f, u.Signature + 0.55f);
                        duel += kills;
                        t.Supp = Math.Min(1f, t.Supp + 0.12f);
                    }
                }
                else
                {
                    if (u.MAmmo > 0.05f)
                    {
                        float mgKill = u.Units * ospec.Mg * 1.05f * k * (1f - cover * 0.85f);
                        float heKill = 0f;
                        if (u.CAmmo > 0.05f)
                        {
                            heKill = u.Units * ospec.He * 4.5f * k * (1f - cover * 0.55f);
                            u.CAmmo = Math.Max(0f, u.CAmmo - u.Units * 0.04f);
                            u.Signature = Math.Min(1f, u.Signature + 0.5f);
                        }
                        ApplyDamage(ref t, mgKill + heKill, foeIntel, best);
                        u.MAmmo = Math.Max(0f, u.MAmmo - u.Units * 0.06f);
                        u.Signature = Math.Min(1f, u.Signature + 0.22f);
                        t.Supp = Math.Min(1f, t.Supp + 0.3f);
                    }
                }
            }
            else
            {
                if (t.Type == 0)
                {
                    if (u.MAmmo > 0.05f)
                    {
                        float kills = u.Units * 0.045f * k * (1f - cover * 0.8f);
                        ApplyDamage(ref t, kills, foeIntel, best);
                        u.MAmmo = Math.Max(0f, u.MAmmo - u.Units * 0.045f);
                        u.Signature = Math.Min(1f, u.Signature + 0.16f);
                        t.Supp = Math.Min(1f, t.Supp + 0.15f);
                    }
                }
                else if (bestDist < 0.45f)
                {
                    float kills = u.Units * 0.0045f * k * (foeStrat == 2 ? 1.2f : 1f);
                    ApplyDamage(ref t, kills, foeIntel, best);
                    u.MAmmo = Math.Max(0f, u.MAmmo - u.Units * 0.02f);
                    duel += kills * 0.5f;
                }
            }
        }
        return duel;
    }

    static void ApplyDamage(ref Group t, float kills, Intel[] foeIntel, int idx)
    {
        if (kills <= 0f) return;
        t.Units = Math.Max(0f, t.Units - kills);
        t.Morale = Math.Max(0f, t.Morale - kills / Math.Max(1f, t.Size0) * 1.6f);
        if (t.Units < t.Size0 * 0.08f || t.Units < 0.5f)
        {
            t.Alive = false;
            foeIntel[idx].Level = 0f;
        }
    }

    static void MoraleSide(Group[] g, int n, bool atk, ref XorRng rng, ref int evtN, int tick)
    {
        for (int i = 0; i < n; i++)
        {
            ref Group u = ref g[i];
            if (!u.Alive) continue;
            u.Supp = Math.Max(0f, u.Supp - 0.08f);
            u.Morale = Math.Min(1f, u.Morale + 0.004f);
            bool active = u.Posture is P_ADVANCE or P_ASSAULT or P_FLANK or P_RETREAT;
            u.Fatigue = Math.Clamp(u.Fatigue + (active ? 0.006f : -0.004f), 0f, 1f);
            if (u.Supp > 0.1f) u.Exp = Math.Min(1f, u.Exp + 0.003f);
            float lossR = 1f - u.Units / Math.Max(1f, u.Size0);
            if (lossR > 0.5f && u.Morale < 0.3f && rng.NextF() < 0.12f)
            {
                if (u.Posture != P_RETREAT) AddEvt(ref evtN, tick, E_ROUT, atk ? 0 : 1);
                u.Posture = P_RETREAT;
                u.TgtY = atk ? -5f : Math.Min(DEPTH_KM, u.Y + 8f);
            }
        }
    }

    static float EffectiveDepth(Group[] a, int n)
    {
        float best = 0f;
        for (int i = 0; i < n; i++)
        {
            if (!a[i].Alive || a[i].Posture == P_RETREAT) continue;
            if (a[i].Y <= best) continue;
            float pw = a[i].Type == 1 ? a[i].Units * 10f : a[i].Units;
            if (pw < 25f) continue;
            for (int j = 0; j < n; j++)
            {
                if (j == i || !a[j].Alive || a[j].Posture == P_RETREAT) continue;
                float dx = a[i].X - a[j].X, dy = a[i].Y - a[j].Y;
                if (dx * dx + dy * dy < 12.25f) { best = a[i].Y; break; }
            }
        }
        return Math.Max(0f, best);
    }

    static float SidePower(Group[] g, int n, TankSpec spec)
    {
        float p = 0f;
        for (int i = 0; i < n; i++)
        {
            if (!g[i].Alive) continue;
            float ammoR = (g[i].CAmmo + g[i].MAmmo) / Math.Max(0.01f, g[i].Size0 * 2f);
            float am = 0.45f + 0.55f * Math.Clamp(ammoR * 1.6f, 0f, 1f);
            p += (g[i].Type == 1 ? g[i].Units * (8f + spec.Armor * 0.04f + spec.Pen * 0.04f) : g[i].Units * 0.85f) * am;
        }
        return p;
    }

    static void CountLosses(Group[] g, int n, ref long tankLoss, ref long soldLoss)
    {
        double tl = 0, sl = 0;
        for (int i = 0; i < n; i++)
        {
            double lost = g[i].Size0 - (g[i].Alive ? g[i].Units : 0);
            if (g[i].Type == 1) tl += lost; else sl += lost;
        }
        tankLoss = (long)Math.Round(tl); soldLoss = (long)Math.Round(sl);
    }

    // ═════════════════════════ ساخت گزارش فارسی ═════════════════════════════
    static string ProgressBar(float frac, int color)
    {
        int filled = (int)Math.Round(Math.Clamp(frac, 0f, 1f) * 10);
        string fill = color == 1 ? "🟩" : color == 2 ? "🟥" : "🟦";
        var sb = new StringBuilder(24);
        for (int i = 0; i < 10; i++) sb.Append(i < filled ? fill : "⬜");
        return sb.ToString();
    }

    static int AttackerColor(BattleResult r) => r.AttackerWon ? 1 : r.AttackerFailed ? 2 : 0;
    static int DefenderColor(BattleResult r) => r.AttackerFailed ? 1 : r.AttackerWon ? 2 : 0;

    static void BuildReports(BattleResult r, Country atk, Country def, TankSpec aSpec, TankSpec dSpec,
        int aStrat, int aTac, int dStrat, int dTac, long aTanks, long aSold, long dTanks, long dSold,
        long aFight, long aBomb, long dFight, long dAA,
        int aAirStrat, int aAirTac, int dAirStrat, int dAirTac, AirOutcome air,
        int evtN, int duelPeakTick, bool encircled, bool ambushFired,
        float aIntelQ, float dIntelQ, float depth, float frac,
        bool anyGround, bool defHasGround, bool supplyStrain, float counterAtk)
    {
        var sb = _sb; sb.Clear();
        var aFs = FighterOf(atk.Faction); var aBs = BomberOf(atk.Faction); var dFs = FighterOf(def.Faction);
        string aStratName = aStrat == 1 ? "هجوم منسجم" : "محاصره و ضربه";
        string aTacName = aStrat == 1
            ? (aTac == 1 ? "حمله مستقیم متمرکز" : "حملات سبک اکتشافی و یورش اصلی")
            : (aTac == 1 ? "محاصره گسترده و فرسایش" : "حلقه محاصره متحرک");
        string dStratName = dStrat == 1 ? "دفاع منسجم" : "دفاع و ضدحمله پراکنده";
        string dTacName = dStrat == 1
            ? (dTac == 1 ? "دفاع ثابت در سنگرها" : "گشت متحرک ترکیبی")
            : (dTac == 1 ? "استتار و کمین" : "عقب‌نشینی تاکتیکی و تله");
        string aAirName = aAirStrat == 1 ? "برتری هوایی" : aAirStrat == 2 ? "بمباران راهبردی" : "بدون عملیات هوایی";
        string aAirTacName = aAirStrat == 1
            ? (aAirTac == 1 ? "شکار آزاد" : "حمله به پایگاه‌ها")
            : aAirStrat == 2 ? (aAirTac == 1 ? "بمباران دقیق" : "بمباران منطقه‌ای") : "—";
        string dAirName = dAirStrat == 1 ? "دفاع منطقه‌ای" : "دفاع نقطه‌ای";
        string dAirTacName = dAirStrat == 1
            ? (dAirTac == 1 ? "گشت هوایی رزمی (CAP)" : "ایستگاه شنود")
            : (dAirTac == 1 ? "آتشبند" : "پوشش مستقیم جنگنده");
        string outcome;
        if (!anyGround)
        {
            outcome = air.Superiority > 0.12 ? $"🛫 عملیات هوایی موفق {atk.Name}"
                    : air.Superiority < -0.12 ? $"🛫 عملیات هوایی ناموفق — برتری با {def.Name}"
                    : "🛫 عملیات هوایی بی‌نتیجه";
        }
        else if (r.AttackerWon) outcome = $"🏆 پیروزی مطلق {atk.Name}";
        else if (r.AttackerFailed) outcome = $"🛡 دفاع کامل {def.Name} — شکست حمله";
        else outcome = $"⚖️ موفقیت {r.SuccessPercent}٪ مهاجم";
        int h = r.DurationMinutes / 60, m = r.DurationMinutes % 60;
        string envLine = $"🌦 آب‌وهوا: {WeatherName[_weather]} | 🕓 آغاز نبرد: {TimeName[_startTime]}";
        string barAtk = $"{ProgressBar(frac, AttackerColor(r))} {r.SuccessPercent}٪";
        string barDef = $"{ProgressBar(frac, DefenderColor(r))} {r.SuccessPercent}٪";
        string TickTime(short t) { int mm = (int)(t * TICK_MIN); return $"{mm / 60}:{mm % 60:D2}"; }
        string firstContact = anyGround && defHasGround ? "تماس آتش در ساعات نخست برقرار شد" : "نبرد زمینی شکل نگرفت";
        string ambushLine = null, breakLine = null, shiftLine = null, routLine = null, haltLine = null, supplyLine = null;
        for (int i = 0; i < evtN; i++)
        {
            var e = _evts[i];
            switch (e.Kind)
            {
                case E_CONTACT: firstContact = $"نخستین تماس آتش در ساعت {TickTime(e.Tick)} در کیلومتر {e.A:F0} جبهه رخ داد"; break;
                case E_AMBUSH: ambushLine ??= $"در ساعت {TickTime(e.Tick)} کمین مدافع در عمق {e.B:F1} کیلومتری فعال شد و ستون پیشرو را درو کرد"; break;
                case E_BREAK5: breakLine ??= $"خط اول دفاع در ساعت {TickTime(e.Tick)} شکست و رخنه ۵ کیلومتری شکل گرفت"; break;
                case E_BREAK10: breakLine = $"در ساعت {TickTime(e.Tick)} رخنه به عمق ۱۰ کیلومتر توسعه یافت"; break;
                case E_BREAK20: breakLine = $"در ساعت {TickTime(e.Tick)} ستون زرهی مهاجم به عمق ۲۰ کیلومتری رسید"; break;
                case E_BREAK30: breakLine = $"در ساعت {TickTime(e.Tick)} عمق ۳۰ کیلومتر درنوردیده شد — فروپاشی جبهه"; break;
                case E_SHIFT: shiftLine ??= $"نقطه عطف نبرد در ساعت {TickTime(e.Tick)} رقم خورد و ابتکار عمل جابه‌جا شد"; break;
                case E_ROUT: routLine ??= e.A < 0.5f ? "چند گروه مهاجم با تلفات سنگین از خط گریختند" : "بخشی از یگان‌های مدافع تار و مار شدند"; break;
                case E_HALT: haltLine ??= $"پیشروی در عمق {e.A:F1} کیلومتری زمین‌گیر شد و جبهه به بن‌بست رسید"; break;
                case E_SUPPLY: supplyLine ??= $"در ساعت {TickTime(e.Tick)} کشش خط تدارکات، آهنگ پیشروی را کند کرد"; break;
            }
        }
        string airLine = BuildAirNarrative(air, aFight, aBomb, dFight, dAA, aAirStrat, aAirTac, aFs, aBs, dFs);
        string counterLine = counterAtk > 1.05f ? "انتخاب استراتژی مهاجم برابر دفاع دشمن، برتری تاکتیکی ایجاد کرد"
                           : counterAtk < 0.95f ? "استراتژی دفاعی دشمن، نقطه‌ضعف رویکرد مهاجم را هدف گرفت" : null;
        string armorLine = aTanks > 0
            ? $"زره‌پوش‌های {aSpec.Name} ستون فقرات حمله بودند؛ {r.AttackerTanksLost} دستگاه از {aTanks} نابود شد"
            : "مهاجم بدون پشتیبانی زرهی جنگید";
        string defArmorLine = dTanks > 0
            ? $"تانک‌های {dSpec.Name} مدافع {r.DefenderTanksLost} دستگاه از دست دادند"
            : "مدافع هیچ زرهی در خط نداشت";
        string infLine = $"پیاده‌نظام سنگین‌ترین تلفات را داد ({r.AttackerSoldiersLost + r.DefenderSoldiersLost} نفر در مجموع)";
        string intelLine = aIntelQ > dIntelQ + 0.12f
            ? "برتری شناسایی با مهاجم بود و آتش او دقیق‌تر نشست"
            : dIntelQ > aIntelQ + 0.12f
            ? "مه جنگ به سود مدافع کار کرد؛ مهاجم اغلب کورکورانه شلیک می‌کرد"
            : "هیچ طرفی برتری اطلاعاتی قاطع نداشت";
        string whyLine = r.AttackerWon
            ? "تمرکز قوا روی ضعیف‌ترین سکتور و توسعه سریع رخنه، کار دفاع را تمام کرد"
            : r.AttackerFailed
            ? "آتش دفاعی سازمان‌یافته و زمینِ مساعد، حمله را پیش از شکل‌گیری رخنه خفه کرد"
            : "هیچ طرف نتوانست ضربه قاطع بزند و نبرد با نتیجه‌ای نسبی پایان یافت";

        // ───────── گزارش مهاجم ─────────
        sb.Append("⚔️ گزارش نبرد — ").Append(atk.Name).Append(" علیه ").Append(def.Name).Append('\n');
        sb.Append(outcome).Append('\n');
        sb.Append(envLine).Append('\n');
        if (anyGround) sb.Append("📊 پیشروی: ").Append(barAtk).Append('\n');
        sb.Append('\n');
        sb.Append("📜 شرح نبرد:\n");
        sb.Append("• استراتژی زمینی: ").Append(aStratName).Append(" / ").Append(aTacName).Append('\n');
        if (aFight > 0 || aBomb > 0) sb.Append("• استراتژی هوایی: ").Append(aAirName).Append(" / ").Append(aAirTacName).Append('\n');
        if (airLine != null) sb.Append("• ").Append(airLine).Append('\n');
        if (anyGround && defHasGround)
        {
            sb.Append("• ").Append(firstContact).Append('\n');
            if (counterLine != null) sb.Append("• ").Append(counterLine).Append('\n');
            if (ambushLine != null) sb.Append("• ").Append(ambushLine).Append('\n');
            if (breakLine != null) sb.Append("• ").Append(breakLine).Append('\n');
            if (encircled) sb.Append("• حلقه محاصره بسته شد و فشار از چند جهت بر مدافع وارد آمد\n");
            if (supplyLine != null) sb.Append("• ").Append(supplyLine).Append('\n');
            if (shiftLine != null) sb.Append("• ").Append(shiftLine).Append('\n');
            if (routLine != null) sb.Append("• ").Append(routLine).Append('\n');
            if (haltLine != null) sb.Append("• ").Append(haltLine).Append('\n');
            sb.Append("• ").Append(armorLine).Append('\n');
            sb.Append("• ").Append(defArmorLine).Append('\n');
            sb.Append("• ").Append(infLine).Append('\n');
            sb.Append("• ").Append(intelLine).Append('\n');
            sb.Append("• ").Append(whyLine).Append('\n');
        }
        else if (!anyGround) sb.Append("• این یک عملیات کاملاً هوایی بود؛ نیروی زمینی اعزام نشد\n");
        else if (!defHasGround) sb.Append("• مدافع نیروی زمینی در خط نداشت و ستون مهاجم تقریباً بی‌مقاومت پیش رفت\n");
        sb.Append('\n');
        sb.Append("📊 آمار نهایی:\n");
        sb.Append($"🔻 تلفات خودی: {r.AttackerTanksLost} تانک، {r.AttackerSoldiersLost} سرباز");
        if (aFight > 0 || aBomb > 0) sb.Append($"، {r.AttackerFightersLost} جنگنده، {r.AttackerBombersLost} بمب‌افکن");
        sb.Append('\n');
        sb.Append($"🔻 تلفات دشمن: {r.DefenderTanksLost} تانک، {r.DefenderSoldiersLost} سرباز");
        if (dFight > 0 || dAA > 0) sb.Append($"، {r.DefenderFightersLost} جنگنده، {r.DefenderAntiAirLost} پدافند");
        sb.Append('\n');
        if (aFight > 0 || aBomb > 0 || dFight > 0 || dAA > 0)
            sb.Append($"🛫 برتری هوایی: {AirSupText(air.Superiority)}\n");
        if (anyGround)
            sb.Append($"📍 نفوذ موثر: {r.PenetrationKm:F1} کیلومتر ({r.SuccessPercent}٪)\n");
        if (anyGround && (r.AttackerMoneyGained > 0 || r.AttackerIronGained > 0))
            sb.Append($"💰 غنیمت (از پیشروی زمینی): {r.AttackerMoneyGained / 1000.0:F1}K پول، {r.AttackerIronGained / 1000.0:F1}K آهن\n");
        else if (anyGround)
            sb.Append("💰 غنیمت: بدون غنیمت (غارت فقط با پیشروی زمینی به‌دست می‌آید)\n");
        if (air.StratMoney > 0 || air.StratIron > 0)
            sb.Append($"🏭 خسارت بمباران به اقتصاد دشمن: {air.StratMoney / 1000.0:F1}K پول، {air.StratIron / 1000.0:F1}K آهن (نابود شد، غنیمت نیست)\n");
        sb.Append($"⏱ مدت نبرد: {h} ساعت و {m} دقیقه");
        r.AttackerReport = sb.ToString();

        // ───────── گزارش مدافع ─────────
        sb.Clear();
        sb.Append("🛡 گزارش دفاع — حمله ").Append(atk.Name).Append(" به ").Append(def.Name).Append('\n');
        sb.Append(outcome).Append('\n');
        sb.Append(envLine).Append('\n');
        if (anyGround) sb.Append("📊 پیشروی دشمن: ").Append(barDef).Append('\n');
        sb.Append('\n');
        sb.Append("📜 شرح نبرد:\n");
        sb.Append("• استراتژی دفاعی شما: ").Append(dStratName).Append(" / ").Append(dTacName).Append('\n');
        if (dFight > 0 || dAA > 0) sb.Append("• دفاع هوایی شما: ").Append(dAirName).Append(" / ").Append(dAirTacName).Append('\n');
        if (airLine != null) sb.Append("• ").Append(airLine).Append('\n');
        if (anyGround && defHasGround)
        {
            sb.Append("• ").Append(firstContact).Append('\n');
            if (ambushLine != null) sb.Append("• ").Append(ambushLine).Append('\n');
            if (breakLine != null) sb.Append("• ").Append(breakLine).Append('\n');
            if (encircled) sb.Append("• دشمن موفق شد حلقه محاصره را ببندد\n");
            if (supplyLine != null) sb.Append("• ").Append(supplyLine).Append('\n');
            if (routLine != null) sb.Append("• ").Append(routLine).Append('\n');
            if (haltLine != null) sb.Append("• ").Append(haltLine).Append('\n');
            sb.Append("• ").Append(defArmorLine).Append('\n');
            sb.Append("• ").Append(infLine).Append('\n');
            sb.Append("• ").Append(intelLine).Append('\n');
            sb.Append("• ").Append(whyLine).Append('\n');
        }
        else if (!anyGround) sb.Append("• حملهٔ دشمن کاملاً هوایی بود\n");
        else if (!defHasGround) sb.Append("• شما نیروی زمینی در خط نداشتید و دشمن آزادانه نفوذ کرد\n");
        sb.Append('\n');
        sb.Append("📊 آمار نهایی:\n");
        sb.Append($"🔻 تلفات خودی: {r.DefenderTanksLost} تانک، {r.DefenderSoldiersLost} سرباز");
        if (dFight > 0 || dAA > 0) sb.Append($"، {r.DefenderFightersLost} جنگنده، {r.DefenderAntiAirLost} پدافند");
        sb.Append('\n');
        sb.Append($"🔻 تلفات دشمن: {r.AttackerTanksLost} تانک، {r.AttackerSoldiersLost} سرباز");
        if (aFight > 0 || aBomb > 0) sb.Append($"، {r.AttackerFightersLost} جنگنده، {r.AttackerBombersLost} بمب‌افکن");
        sb.Append('\n');
        if (aFight > 0 || aBomb > 0 || dFight > 0 || dAA > 0)
            sb.Append($"🛫 برتری هوایی: {AirSupText(air.Superiority)}\n");
        if (anyGround)
            sb.Append($"📍 نفوذ دشمن: {r.PenetrationKm:F1} کیلومتر ({r.SuccessPercent}٪)\n");
        sb.Append($"💸 خسارت کل: {r.DefenderMoneyLost / 1000.0:F1}K پول، {r.DefenderIronLost / 1000.0:F1}K آهن\n");
        sb.Append($"⏱ مدت نبرد: {h} ساعت و {m} دقیقه");
        r.DefenderReport = sb.ToString();

        // ───────── اعلامیه گروه ─────────
        sb.Clear();
        sb.Append("📰 خبر جنگ!\n");
        sb.Append($"⚔️ {atk.Name} به {def.Name} حمله کرد!\n");
        sb.Append(outcome).Append('\n');
        sb.Append(anyGround ? $"🌦 {WeatherName[_weather]} | 📊 {barAtk}\n" : $"🌦 {WeatherName[_weather]} | 🛫 عملیات هوایی\n");
        if (aFight > 0 || aBomb > 0 || dFight > 0 || dAA > 0)
            sb.Append($"🛫 برتری هوایی: {AirSupText(air.Superiority)}\n");
        if (anyGround)
            sb.Append($"📍 نفوذ: {r.PenetrationKm:F1} کیلومتر | ⏱ {h}:{m:D2}\n");
        else
            sb.Append($"⏱ {h}:{m:D2}\n");
        sb.Append($"💀 تلفات مهاجم: {r.AttackerTanksLost}🛡 {r.AttackerSoldiersLost}🪖 {r.AttackerFightersLost}✈️ {r.AttackerBombersLost}🛩️\n");
        sb.Append($"💀 تلفات مدافع: {r.DefenderTanksLost}🛡 {r.DefenderSoldiersLost}🪖 {r.DefenderFightersLost}✈️ {r.DefenderAntiAirLost}🎯");
        if (r.AttackerMoneyGained > 0)
            sb.Append($"\n💰 غنیمت: {r.AttackerMoneyGained / 1000.0:F1}K پول، {r.AttackerIronGained / 1000.0:F1}K آهن");
        r.GroupAnnouncement = sb.ToString();
    }

    static string AirSupText(double sup)
    {
        if (sup > 0.4) return "قاطع با مهاجم 🟢";
        if (sup > 0.12) return "نسبی با مهاجم";
        if (sup < -0.4) return "قاطع با مدافع 🔴";
        if (sup < -0.12) return "نسبی با مدافع";
        return "متوازن ⚪";
    }

    static string BuildAirNarrative(AirOutcome air, long aFight, long aBomb, long dFight, long dAA,
        int aAirStrat, int aAirTac, FighterSpec aFs, BomberSpec aBs, FighterSpec dFs)
    {
        if (aFight == 0 && aBomb == 0 && dFight == 0 && dAA == 0) return null;
        var s = new StringBuilder();
        if (air.HadAirCombat)
            s.Append($"در نبرد هوا‌به‌هوا، جنگنده‌های {aFs.Name} مهاجم با {dFs.Name} مدافع درگیر شدند؛ ")
             .Append($"{air.AtkFightersLost} جنگندهٔ مهاجم و {air.DefFightersLost} جنگندهٔ مدافع سرنگون شد. ");
        else if (aFight > 0 && dFight == 0)
            s.Append($"جنگنده‌های {aFs.Name} مهاجم بدون مقاومت هوایی، آسمان را در اختیار گرفتند. ");
        if (dAA > 0 && (aBomb > 0 || aFight > 0))
            s.Append($"آتش پدافند ضدهوایی {air.AtkBombersLost} بمب‌افکن را سرنگون کرد و خود {air.DefAntiAirLost} قبضه از دست داد. ");
        if (aAirStrat == 2 && (air.StratMoney > 0 || air.StratIron > 0))
            s.Append(aAirTac == 1
                ? $"بمباران دقیق صنایع، خسارت سنگینی به اقتصاد دشمن زد ({air.StratMoney / 1000.0:F1}K پول، {air.StratIron / 1000.0:F1}K آهن). "
                : $"بمباران فرشی شهرها، زیرساخت و روحیهٔ دشمن را درهم کوبید ({air.StratMoney / 1000.0:F1}K پول). ");
        else if (aAirStrat == 1 && air.Superiority > 0.15)
            s.Append("با کسب برتری هوایی، پشتیبانی نزدیک هوایی به نفع پیشروی زمینی وارد عمل شد. ");
        else if (air.Superiority < -0.15)
            s.Append("برتری هوایی به دست مدافع افتاد و فشار هوایی بر مهاجم سنگینی کرد. ");
        return s.Length > 0 ? s.ToString().TrimEnd() : null;
    }

    // ═════════════════════════ ذخیره در دیتابیس ═════════════════════════════
    static int _dbReady;
    static void SaveBattle(Country atk, Country def, BattleResult r)
    {
        try
        {
            using var con = new SqliteConnection("Data Source=gamedata.db");
            con.Open();
            if (Interlocked.CompareExchange(ref _dbReady, 1, 0) == 0)
            {
                using var init = con.CreateCommand();
                init.CommandText = @"
                CREATE TABLE IF NOT EXISTS WarBattles(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    ChatId INTEGER, AttackerId INTEGER, DefenderId INTEGER,
                    AttackerName TEXT, DefenderName TEXT,
                    Winner TEXT, PenetrationKm REAL, SuccessPercent INTEGER,
                    AtkTankLoss INTEGER, AtkSoldierLoss INTEGER,
                    DefTankLoss INTEGER, DefSoldierLoss INTEGER,
                    LootMoney INTEGER, LootIron INTEGER,
                    DurationMinutes INTEGER, Report TEXT
                );";
                init.ExecuteNonQuery();
            }
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"INSERT INTO WarBattles
                (Timestamp,ChatId,AttackerId,DefenderId,AttackerName,DefenderName,Winner,
                 PenetrationKm,SuccessPercent,AtkTankLoss,AtkSoldierLoss,DefTankLoss,DefSoldierLoss,
                 LootMoney,LootIron,DurationMinutes,Report)
                VALUES (@ts,@chat,@aid,@did,@an,@dn,@w,@pen,@sp,@atl,@asl,@dtl,@dsl,@lm,@li,@dur,@rep)";
            cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@chat", atk.ChatId);
            cmd.Parameters.AddWithValue("@aid", atk.OwnerId);
            cmd.Parameters.AddWithValue("@did", def.OwnerId);
            cmd.Parameters.AddWithValue("@an", atk.Name);
            cmd.Parameters.AddWithValue("@dn", def.Name);
            cmd.Parameters.AddWithValue("@w", r.AttackerWon ? atk.Name : r.AttackerFailed ? def.Name : $"نسبی {r.SuccessPercent}%");
            cmd.Parameters.AddWithValue("@pen", r.PenetrationKm);
            cmd.Parameters.AddWithValue("@sp", r.SuccessPercent);
            cmd.Parameters.AddWithValue("@atl", r.AttackerTanksLost);
            cmd.Parameters.AddWithValue("@asl", r.AttackerSoldiersLost);
            cmd.Parameters.AddWithValue("@dtl", r.DefenderTanksLost);
            cmd.Parameters.AddWithValue("@dsl", r.DefenderSoldiersLost);
            cmd.Parameters.AddWithValue("@lm", r.AttackerMoneyGained);
            cmd.Parameters.AddWithValue("@li", r.AttackerIronGained);
            cmd.Parameters.AddWithValue("@dur", r.DurationMinutes);
            cmd.Parameters.AddWithValue("@rep", r.AttackerReport);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }
}
