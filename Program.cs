// ===== /root/CountryBot/Program.cs =====
// Fixed version — daily update timer properly awaits, group messages + DB backup to admin
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InputFiles;
using Microsoft.Data.Sqlite;

enum Faction { USSR, USA, Reich }

class Country
{
    public string Name { get; set; } = "";
    public long OwnerId { get; set; }
    public long ChatId { get; set; }
    public string OwnerName { get; set; } = "";
    public Faction Faction { get; set; }
    public string FlagFileId { get; set; } = "";
    public long Money { get; set; } = 10000;
    public long Population { get; set; } = 100000;
    public int Cities { get; set; } = 4;
    public int FactoryLevel { get; set; } = 1;
    public int PortLevel { get; set; } = 1;
    public int MineLevel { get; set; } = 1;
    public long Iron { get; set; } = 0;
    public long Soldiers { get; set; } = 10000;
    public long Tanks { get; set; } = 0;
    public long Planes { get; set; } = 0;
    public long Bombers { get; set; } = 0;
    public long AntiAir { get; set; } = 0;
    public long DefenseTanks { get; set; } = 0;
    public long DefenseSoldiers { get; set; } = 0;
    public long DefenseFighters { get; set; } = 0;
    public int AirDefStrategy { get; set; } = 1;
    public int AirDefTactic { get; set; } = 1;
    public int Besieged { get; set; } = 0;
    public int DefenseWins { get; set; } = 0;
    public long CreatedAtMs { get; set; } = 0;
    public int DefenseStrategy { get; set; } = 1;
    public int DefenseTactic { get; set; } = 1;
    public int RecruitmentRate { get; set; } = 0;
    public double Welfare { get; set; } = 100;
    public int TaxRate { get; set; } = 30;
    public int DefTankPct { get; set; } = 100;
    public int DefSoldierPct { get; set; } = 100;
    public int DefFighterPct { get; set; } = 100;
}

class Alliance
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public string Name { get; set; } = "";
    public string FlagFileId { get; set; } = "";
    public long LeaderId { get; set; }
    public long CreatedAtMs { get; set; }
}

class AllianceInvite
{
    public long Id { get; set; }
    public long AllianceId { get; set; }
    public long ChatId { get; set; }
    public long TargetUserId { get; set; }
    public long LeaderId { get; set; }
    public long CreatedAtMs { get; set; }
}

class Transfer
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public long AllianceId { get; set; }
    public long SenderId { get; set; }
    public long ReceiverId { get; set; }
    public string ResourceType { get; set; } = "";
    public long Amount { get; set; }
    public long ArriveAtMs { get; set; }
    public int Notified { get; set; }
}

class Deployment
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public long AllianceId { get; set; }
    public long InitiatorId { get; set; }
    public long TargetUserId { get; set; }
    public string Type { get; set; } = "";
    public int DurationHours { get; set; }
    public string FormationType { get; set; } = "";
    public int Strategy { get; set; } = 1;
    public int Tactic { get; set; } = 1;
    public long Tanks { get; set; }
    public long Soldiers { get; set; }
    public long Fighters { get; set; }
    public long Bombers { get; set; }
    public long CreatedAtMs { get; set; }
    public long EndAtMs { get; set; }
    public long LastWarnMs { get; set; }
    // FIX(2): پیام اعلام صف‌آرایی که در گروه پین می‌شود تا هنگام لغو/پایان آنپین و حذف شود
    public int AnnounceMsgId { get; set; } = 0;
}

class DeploymentContributor
{
    public long Id { get; set; }
    public long DeploymentId { get; set; }
    public long UserId { get; set; }
    public long Tanks { get; set; }
    public long Soldiers { get; set; }
    public long Fighters { get; set; }
    public long Bombers { get; set; }
    public int Strategy { get; set; } = 1;
    public int Tactic { get; set; } = 1;
}

enum SessionStep
{
    None,
    WaitingCountryName,
    WaitingNewName,
    WaitingNewFlag,
    OwnerWaitingFlagManage,
    OwnerWaitingDailyTime,
    OwnerWaitingMinuteTime,
    WaitingDeleteConfirm,
    OwnerWaitingSpecialPhoto,
    OwnerWaitingNewDatabase,
    OwnerWaitingAnnounceAll,
    OwnerWaitingAnnouncePrivate,
    OwnerWaitingAnnounceGroup,
    OwnerWaitingVisionSource,
    OwnerWaitingVisionConfirm,
    WaitingRecruitmentRate,
    WaitingTaxRate,
    WaitingTradeAmount,
    OwnerWaitingRoyalDeposit,
    OwnerWaitingRoyalDepositAmount,
    OwnerWaitingRoyalDeduct,
    OwnerWaitingRoyalDeductAmount,
    AttackWaitingGroup,
    AttackWaitingTarget,
    AttackWaitingStrategy,
    AttackWaitingTactic,
    AttackWaitingTanks,
    AttackWaitingSoldiers,
    AttackWaitingFighters,
    AttackWaitingBombers,
    AttackWaitingAirStrategy,
    AttackWaitingAirTactic,
    DefenseWaitingGroup,
    DefenseWaitingStrategy,
    DefenseWaitingTactic,
    DefenseWaitingTanks,
    DefenseWaitingSoldiers,
    DefenseWaitingFighters,
    WaitingAllianceName,
    WaitingAllianceFlag,
    LeaderWaitingKickMember,
    TransferWaitingChat,
    TransferWaitingResource,
    TransferWaitingTarget,
    TransferWaitingDuration,
    TransferWaitingAmount,
    DeployWaitingChat,
    DeployWaitingTarget,
    DeployWaitingDuration,
    DeployWaitingFormation,
    DeployWaitingStrategy,
    DeployWaitingTactic,
    DeployWaitingTanks,
    DeployWaitingSoldiers,
    DeployWaitingFighters,
    DeployWaitingBombers,
    DeployJoinWaitingStrategy,
    DeployJoinWaitingTactic,
    DeployJoinWaitingTanks,
    DeployJoinWaitingSoldiers,
    DeployJoinWaitingFighters,
    DeployJoinWaitingBombers,
}

class UserSession
{
    public SessionStep Step { get; set; } = SessionStep.None;
    public long AllianceChatId { get; set; } = 0;
    public long AllianceId { get; set; } = 0;
    public string AllianceName { get; set; } = "";
    public long TransferChatId { get; set; } = 0;
    public long TransferAllianceId { get; set; } = 0;
    public string TransferResourceType { get; set; } = "";
    public long TransferTargetId { get; set; } = 0;
    public int TransferDurationMin { get; set; } = 0;
    public long VisionDestChatId { get; set; } = 0;
    public long VisionSourceId { get; set; } = 0;
    public long DeployChatId { get; set; } = 0;
    public long DeployAllianceId { get; set; } = 0;
    public string DeployType { get; set; } = "";
    public long DeployTargetId { get; set; } = 0;
    public int DeployDuration { get; set; } = 0;
    public string DeployFormation { get; set; } = "";
    public int DeployStrategy { get; set; } = 1;
    public int DeployTactic { get; set; } = 1;
    public long DeployTanks { get; set; } = 0;
    public long DeploySoldiers { get; set; } = 0;
    public long DeployFighters { get; set; } = 0;
    public long DeployBombers { get; set; } = 0;
    public int DefTankPct { get; set; } = 100;
    public int DefSoldierPct { get; set; } = 100;
    public Faction Faction { get; set; }
    public string FactionStr { get; set; } = "";
    public long PromptChatId { get; set; }
    public int PromptMsgId { get; set; }
    public long ChatId { get; set; }
    public long AttackTargetId { get; set; } = 0;
    public long AttackChatId { get; set; } = 0;
    public int AttackStrategy { get; set; } = 0;
    public int AttackTactic { get; set; } = 0;
    public long AttackTanks { get; set; } = 0;
    public long AttackSoldiers { get; set; } = 0;
    public long AttackFighters { get; set; } = 0;
    public long AttackBombers { get; set; } = 0;
    public int AttackAirStrategy { get; set; } = 0;
    public int AttackAirTactic { get; set; } = 0;
    public long DefenseTanks { get; set; } = 0;
    public long DefenseSoldiers { get; set; } = 0;
    public int DefenseStrategy { get; set; } = 1;
    public int DefenseTactic { get; set; } = 1;
    public int AnnounceCount { get; set; } = 0;
    public long DeployJoinId { get; set; } = 0;
    public int DeployJoinStrategy { get; set; } = 1;
    public int DeployJoinTactic { get; set; } = 1;
    public long DeployJoinTanks { get; set; } = 0;
    public long DeployJoinSoldiers { get; set; } = 0;
    public long DeployJoinFighters { get; set; } = 0;
    public long DeployJoinBombers { get; set; } = 0;
}

static partial class Database
{
    private const string CONNECTION = "Data Source=gamedata.db";
    private static SqliteConnection OpenCon()
    {
        var con = new SqliteConnection(CONNECTION);
        con.Open();
        using var p = con.CreateCommand();
        p.CommandText = "PRAGMA busy_timeout=5000; PRAGMA journal_mode=WAL;";
        p.ExecuteNonQuery();
        return con;
    }

    public static void Init()
    {
        using var con = OpenCon();
        string countries = @"
        CREATE TABLE IF NOT EXISTS Countries(
            ChatId INTEGER NOT NULL,
            OwnerId INTEGER NOT NULL,
            Name TEXT NOT NULL,
            OwnerName TEXT NOT NULL,
            Faction INTEGER NOT NULL,
            FlagFileId TEXT,
            Money INTEGER NOT NULL DEFAULT 10000,
            Population INTEGER NOT NULL DEFAULT 100000,
            Cities INTEGER DEFAULT 4,
            FactoryLevel INTEGER DEFAULT 1,
            PortLevel INTEGER DEFAULT 1,
            MineLevel INTEGER DEFAULT 1,
            Iron INTEGER DEFAULT 0,
            Soldiers INTEGER DEFAULT 10000,
            RecruitmentRate INTEGER DEFAULT 0,
            Welfare REAL DEFAULT 100,
            Tanks INTEGER DEFAULT 0,
            Planes INTEGER DEFAULT 0,
            Bombers INTEGER DEFAULT 0,
            AntiAir INTEGER DEFAULT 0,
            DefenseFighters INTEGER DEFAULT 0,
            AirDefStrategy INTEGER DEFAULT 1,
            AirDefTactic INTEGER DEFAULT 1,
            Besieged INTEGER DEFAULT 0,
            DefenseWins INTEGER DEFAULT 0,
            CreatedAtMs INTEGER DEFAULT 0,
            DefenseTanks INTEGER DEFAULT 0,
            DefenseSoldiers INTEGER DEFAULT 0,
            DefenseStrategy INTEGER DEFAULT 1,
            DefenseTactic INTEGER DEFAULT 1,
            TaxRate INTEGER DEFAULT 30,
            PRIMARY KEY(ChatId,OwnerId)
        );";
        string flags = @"
        CREATE TABLE IF NOT EXISTS FactionFlags(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Faction TEXT NOT NULL,
            FileId TEXT NOT NULL
        );";
        string settings = @"
        CREATE TABLE IF NOT EXISTS Settings(
            Key TEXT PRIMARY KEY,
            Value TEXT
        );";
        string royal = @"
        CREATE TABLE IF NOT EXISTS RoyalCoins(
            OwnerId INTEGER PRIMARY KEY,
            Amount INTEGER DEFAULT 0
        );";
        string cooldowns = @"
        CREATE TABLE IF NOT EXISTS LeaveCooldowns(
            OwnerId INTEGER NOT NULL,
            ChatId INTEGER NOT NULL,
            UntilUnixMs INTEGER NOT NULL,
            PRIMARY KEY(OwnerId,ChatId)
        );";
        string defeats = @"
        CREATE TABLE IF NOT EXISTS RoutDefeats(
            DefenderId INTEGER NOT NULL,
            ChatId INTEGER NOT NULL,
            AttackerId INTEGER NOT NULL,
            Count INTEGER NOT NULL DEFAULT 0,
            PRIMARY KEY(DefenderId,ChatId,AttackerId)
        );";
        string shieldExemptions = @"
        CREATE TABLE IF NOT EXISTS ShieldExemptions(
            OwnerId INTEGER NOT NULL,
            ChatId INTEGER NOT NULL,
            PRIMARY KEY(OwnerId,ChatId)
        );";
        string alliances = @"
        CREATE TABLE IF NOT EXISTS Alliances(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ChatId INTEGER NOT NULL,
            Name TEXT NOT NULL,
            FlagFileId TEXT,
            LeaderId INTEGER NOT NULL,
            CreatedAtMs INTEGER NOT NULL
        );";
        string allianceMembers = @"
        CREATE TABLE IF NOT EXISTS AllianceMembers(
            AllianceId INTEGER NOT NULL,
            ChatId INTEGER NOT NULL,
            UserId INTEGER NOT NULL,
            PRIMARY KEY(ChatId,UserId)
        );";
        string allianceInvites = @"
        CREATE TABLE IF NOT EXISTS AllianceInvites(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            AllianceId INTEGER NOT NULL,
            ChatId INTEGER NOT NULL,
            TargetUserId INTEGER NOT NULL,
            LeaderId INTEGER NOT NULL,
            CreatedAtMs INTEGER NOT NULL
        );";
        string transfers = @"
        CREATE TABLE IF NOT EXISTS Transfers(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ChatId INTEGER NOT NULL,
            AllianceId INTEGER NOT NULL,
            SenderId INTEGER NOT NULL,
            ReceiverId INTEGER NOT NULL,
            ResourceType TEXT NOT NULL,
            Amount INTEGER NOT NULL,
            ArriveAtMs INTEGER NOT NULL,
            Notified INTEGER DEFAULT 0
        );";
        string deployments = @"
        CREATE TABLE IF NOT EXISTS Deployments(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ChatId INTEGER NOT NULL,
            AllianceId INTEGER NOT NULL,
            InitiatorId INTEGER NOT NULL,
            TargetUserId INTEGER NOT NULL,
            Type TEXT NOT NULL,
            DurationHours INTEGER NOT NULL,
            FormationType TEXT NOT NULL,
            Strategy INTEGER DEFAULT 1,
            Tactic INTEGER DEFAULT 1,
            Tanks INTEGER DEFAULT 0,
            Soldiers INTEGER DEFAULT 0,
            Fighters INTEGER DEFAULT 0,
            Bombers INTEGER DEFAULT 0,
            CreatedAtMs INTEGER NOT NULL,
            EndAtMs INTEGER NOT NULL,
            LastWarnMs INTEGER DEFAULT 0,
            AnnounceMsgId INTEGER DEFAULT 0
        );";
        string deploymentContributors = @"
        CREATE TABLE IF NOT EXISTS DeploymentContributors(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            DeploymentId INTEGER NOT NULL,
            UserId INTEGER NOT NULL,
            Tanks INTEGER DEFAULT 0,
            Soldiers INTEGER DEFAULT 0,
            Fighters INTEGER DEFAULT 0,
            Bombers INTEGER DEFAULT 0,
            Strategy INTEGER DEFAULT 1,
            Tactic INTEGER DEFAULT 1
        );";
    string visionLogs = @"CREATE TABLE IF NOT EXISTS VisionLogs(Id INTEGER PRIMARY KEY AUTOINCREMENT, SourceChatId INTEGER NOT NULL DEFAULT 0, SourceUserId INTEGER NOT NULL DEFAULT 0, DestChatId INTEGER NOT NULL, IsUserMode INTEGER NOT NULL DEFAULT 0, CreatedAtMs INTEGER NOT NULL);";
    string visionMessageMap = @"CREATE TABLE IF NOT EXISTS VisionMessageMap(Id INTEGER PRIMARY KEY AUTOINCREMENT, SourceChatId INTEGER NOT NULL, SourceMessageId INTEGER NOT NULL, SourceUserId INTEGER NOT NULL DEFAULT 0, DestChatId INTEGER NOT NULL, DestMessageId INTEGER NOT NULL, CreatedAtMs INTEGER NOT NULL);";
        string groupLockExemptions = @"
        CREATE TABLE IF NOT EXISTS GroupLockExemptions(
            ChatId INTEGER PRIMARY KEY
        );";

        string attackAbandonLocks = @"CREATE TABLE IF NOT EXISTS AttackAbandonLocks(OwnerId INTEGER NOT NULL, LockedUntilMs INTEGER NOT NULL, PRIMARY KEY(OwnerId));";
        string dailyDefendCounts = @"CREATE TABLE IF NOT EXISTS DailyDefendCounts(DefenderId INTEGER NOT NULL, AttackDate TEXT NOT NULL, Count INTEGER NOT NULL DEFAULT 0, PRIMARY KEY(DefenderId,AttackDate));";
        string attackerFlags = @"CREATE TABLE IF NOT EXISTS AttackerFlags(OwnerId INTEGER NOT NULL, AttackDate TEXT NOT NULL, PRIMARY KEY(OwnerId, AttackDate));";
        string eqModels = @"CREATE TABLE IF NOT EXISTS EquipmentModels(OwnerId INTEGER NOT NULL, ChatId INTEGER NOT NULL, Category TEXT NOT NULL, ModelName TEXT NOT NULL, Count INTEGER NOT NULL DEFAULT 0, PRIMARY KEY(OwnerId,ChatId,Category,ModelName));";
        foreach (var sql in new[] { countries, flags, settings, royal, cooldowns, defeats, shieldExemptions, alliances, allianceMembers, allianceInvites, transfers, deployments, deploymentContributors, groupLockExemptions, visionLogs, visionMessageMap, attackAbandonLocks, dailyDefendCounts, attackerFlags, eqModels })
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        EnsureColumn(con, "Countries", "Soldiers", "INTEGER DEFAULT 10000");
        EnsureColumn(con, "Countries", "RecruitmentRate", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "Welfare", "REAL DEFAULT 100");
        EnsureColumn(con, "Countries", "PortLevel", "INTEGER DEFAULT 1");
        EnsureColumn(con, "Countries", "MineLevel", "INTEGER DEFAULT 1");
        EnsureColumn(con, "Countries", "Iron", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "Tanks", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "Planes", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "Bombers", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "AntiAir", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "DefenseFighters", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "AirDefStrategy", "INTEGER DEFAULT 1");
        EnsureColumn(con, "Countries", "AirDefTactic", "INTEGER DEFAULT 1");
        EnsureColumn(con, "Countries", "Besieged", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "DefenseWins", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "CreatedAtMs", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "DefenseTanks", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "DefenseSoldiers", "INTEGER DEFAULT 0");
        EnsureColumn(con, "Countries", "DefenseStrategy", "INTEGER DEFAULT 1");
        EnsureColumn(con, "Countries", "DefenseTactic", "INTEGER DEFAULT 1");
        EnsureColumn(con, "Countries", "TaxRate", "INTEGER DEFAULT 30");
        EnsureColumn(con, "Countries", "Cities", "INTEGER DEFAULT 4");
        EnsureColumn(con, "Countries", "DefTankPct", "INTEGER DEFAULT 100");
        EnsureColumn(con, "Countries", "DefSoldierPct", "INTEGER DEFAULT 100");
        EnsureColumn(con, "Countries", "DefFighterPct", "INTEGER DEFAULT 100");
        // FIX(2): ستون جدید برای پیام پین‌شدهٔ صف‌آرایی (روی دیتابیس‌های قدیمی هم اضافه می‌شود)
        EnsureColumn(con, "Deployments", "AnnounceMsgId", "INTEGER DEFAULT 0");

        using (var fix = con.CreateCommand())
        {
            fix.CommandText = @"
                UPDATE Countries SET Population = 100000 WHERE Population IS NULL OR Population < 100000;
                UPDATE Countries SET Money       = 10000 WHERE Money IS NULL;
                UPDATE Countries SET Soldiers    = 10000 WHERE Soldiers IS NULL;
                UPDATE Countries SET Iron        = 0     WHERE Iron IS NULL;
                UPDATE Countries SET Tanks       = 0     WHERE Tanks IS NULL;
                UPDATE Countries SET Planes      = 0     WHERE Planes IS NULL;
                UPDATE Countries SET Bombers     = 0     WHERE Bombers IS NULL;
                UPDATE Countries SET AntiAir     = 0     WHERE AntiAir IS NULL;
                UPDATE Countries SET DefenseFighters = 0 WHERE DefenseFighters IS NULL;
                UPDATE Countries SET AirDefStrategy = 1 WHERE AirDefStrategy IS NULL;
                UPDATE Countries SET AirDefTactic = 1 WHERE AirDefTactic IS NULL;
                UPDATE Countries SET FactoryLevel= 1     WHERE FactoryLevel IS NULL OR FactoryLevel < 1;
                UPDATE Countries SET PortLevel   = 1     WHERE PortLevel IS NULL OR PortLevel < 1;
                UPDATE Countries SET MineLevel   = 1     WHERE MineLevel IS NULL OR MineLevel < 1;
                UPDATE Countries SET RecruitmentRate = 0 WHERE RecruitmentRate IS NULL;
                UPDATE Countries SET Welfare     = 100   WHERE Welfare IS NULL;
                UPDATE Countries SET TaxRate     = 30    WHERE TaxRate IS NULL;
                UPDATE Countries SET Cities      = 4     WHERE Cities IS NULL OR Cities < 1;
                UPDATE Countries SET DefenseStrategy = 1 WHERE DefenseStrategy IS NULL;
                UPDATE Countries SET DefenseTactic   = 1 WHERE DefenseTactic IS NULL;";
            fix.ExecuteNonQuery();
        }
    }

    private static void EnsureColumn(SqliteConnection con, string table, string column, string type)
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        bool exists = false;
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                if (reader.GetString(1) == column) { exists = true; break; }
            }
        }
        if (!exists)
        {
            using var alterCmd = con.CreateCommand();
            alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type}";
            alterCmd.ExecuteNonQuery();
        }
    }

    private const string COUNTRY_COLS =
        "ChatId,OwnerId,Name,OwnerName,Faction,FlagFileId,Money,Population," +
        "FactoryLevel,PortLevel,MineLevel,Iron,Soldiers,RecruitmentRate,Welfare," +
        "Tanks,DefenseTanks,DefenseSoldiers,DefenseStrategy,DefenseTactic,Planes,TaxRate,Cities,Bombers,AntiAir,DefenseFighters,AirDefStrategy,AirDefTactic,Besieged,DefenseWins,CreatedAtMs,DefTankPct,DefSoldierPct,DefFighterPct";

    private static Country ReadCountry(SqliteDataReader r)
    {
        return new Country
        {
            ChatId = r.GetInt64(0),
            OwnerId = r.GetInt64(1),
            Name = r.GetString(2),
            OwnerName = r.GetString(3),
            Faction = (Faction)r.GetInt32(4),
            FlagFileId = r.IsDBNull(5) ? "" : r.GetString(5),
            Money = r.GetInt64(6),
            Population = r.GetInt64(7),
            FactoryLevel = r.IsDBNull(8) ? 1 : r.GetInt32(8),
            PortLevel = r.IsDBNull(9) ? 1 : r.GetInt32(9),
            MineLevel = r.IsDBNull(10) ? 1 : r.GetInt32(10),
            Iron = r.IsDBNull(11) ? 0 : r.GetInt64(11),
            Soldiers = r.IsDBNull(12) ? 10000 : r.GetInt64(12),
            RecruitmentRate = r.IsDBNull(13) ? 0 : r.GetInt32(13),
            Welfare = r.IsDBNull(14) ? 100 : r.GetDouble(14),
            Tanks = r.IsDBNull(15) ? 0 : r.GetInt64(15),
            DefenseTanks = r.IsDBNull(16) ? 0 : r.GetInt64(16),
            DefenseSoldiers = r.IsDBNull(17) ? 0 : r.GetInt64(17),
            DefenseStrategy = r.IsDBNull(18) ? 1 : r.GetInt32(18),
            DefenseTactic = r.IsDBNull(19) ? 1 : r.GetInt32(19),
            Planes = r.IsDBNull(20) ? 0 : r.GetInt64(20),
            TaxRate = r.IsDBNull(21) ? 30 : r.GetInt32(21),
            Cities = r.IsDBNull(22) ? 4 : r.GetInt32(22),
            Bombers = r.IsDBNull(23) ? 0 : r.GetInt64(23),
            AntiAir = r.IsDBNull(24) ? 0 : r.GetInt64(24),
            DefenseFighters = r.IsDBNull(25) ? 0 : r.GetInt64(25),
            AirDefStrategy = r.IsDBNull(26) ? 1 : r.GetInt32(26),
            AirDefTactic = r.IsDBNull(27) ? 1 : r.GetInt32(27),
            Besieged = r.IsDBNull(28) ? 0 : r.GetInt32(28),
            DefenseWins = r.IsDBNull(29) ? 0 : r.GetInt32(29),
            CreatedAtMs = r.IsDBNull(30) ? 0 : r.GetInt64(30),
            DefTankPct = r.FieldCount > 31 && !r.IsDBNull(31) ? r.GetInt32(31) : 100,
            DefSoldierPct = r.FieldCount > 32 && !r.IsDBNull(32) ? r.GetInt32(32) : 100,
            DefFighterPct = r.FieldCount > 33 && !r.IsDBNull(33) ? r.GetInt32(33) : 100,
        };
    }

    public static bool CountryExists(long ownerId, long chatId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Countries WHERE OwnerId=@id AND ChatId=@chat";
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@chat", chatId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public static bool CountryNameExists(string name)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Countries WHERE lower(Name)=lower(@name)";
        cmd.Parameters.AddWithValue("@name", name);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public static void AddCountry(Country c)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
        INSERT INTO Countries
          (ChatId,OwnerId,Name,OwnerName,Faction,FlagFileId,Money,Population,FactoryLevel,PortLevel,MineLevel,Iron,Soldiers,RecruitmentRate,Welfare,Tanks,Planes,DefenseTanks,DefenseSoldiers,DefenseStrategy,DefenseTactic,TaxRate,Cities,Bombers,AntiAir,DefenseFighters,AirDefStrategy,AirDefTactic,Besieged,DefenseWins,CreatedAtMs)
        VALUES
          (@ChatId,@OwnerId,@Name,@OwnerName,@Faction,@FlagFileId,@Money,@Population,@FactoryLevel,@PortLevel,@MineLevel,@Iron,@Soldiers,@RecruitmentRate,@Welfare,@Tanks,@Planes,@DefenseTanks,@DefenseSoldiers,@DefenseStrategy,@DefenseTactic,@TaxRate,@Cities,@Bombers,@AntiAir,@DefenseFighters,@AirDefStrategy,@AirDefTactic,@Besieged,@DefenseWins,@CreatedAtMs)";
        cmd.Parameters.AddWithValue("@ChatId", c.ChatId);
        cmd.Parameters.AddWithValue("@OwnerId", c.OwnerId);
        cmd.Parameters.AddWithValue("@Name", c.Name);
        cmd.Parameters.AddWithValue("@OwnerName", c.OwnerName);
        cmd.Parameters.AddWithValue("@Faction", (int)c.Faction);
        cmd.Parameters.AddWithValue("@FlagFileId", c.FlagFileId);
        cmd.Parameters.AddWithValue("@Money", c.Money);
        cmd.Parameters.AddWithValue("@Population", c.Population);
        cmd.Parameters.AddWithValue("@FactoryLevel", c.FactoryLevel);
        cmd.Parameters.AddWithValue("@PortLevel", c.PortLevel);
        cmd.Parameters.AddWithValue("@MineLevel", c.MineLevel);
        cmd.Parameters.AddWithValue("@Iron", c.Iron);
        cmd.Parameters.AddWithValue("@Soldiers", c.Soldiers);
        cmd.Parameters.AddWithValue("@RecruitmentRate", c.RecruitmentRate);
        cmd.Parameters.AddWithValue("@Welfare", c.Welfare);
        cmd.Parameters.AddWithValue("@Tanks", c.Tanks);
        cmd.Parameters.AddWithValue("@Planes", c.Planes);
        cmd.Parameters.AddWithValue("@DefenseTanks", c.DefenseTanks);
        cmd.Parameters.AddWithValue("@DefenseSoldiers", c.DefenseSoldiers);
        cmd.Parameters.AddWithValue("@DefenseStrategy", c.DefenseStrategy);
        cmd.Parameters.AddWithValue("@DefenseTactic", c.DefenseTactic);
        cmd.Parameters.AddWithValue("@TaxRate", c.TaxRate);
        cmd.Parameters.AddWithValue("@Cities", c.Cities);
        cmd.Parameters.AddWithValue("@Bombers", c.Bombers);
        cmd.Parameters.AddWithValue("@AntiAir", c.AntiAir);
        cmd.Parameters.AddWithValue("@DefenseFighters", c.DefenseFighters);
        cmd.Parameters.AddWithValue("@AirDefStrategy", c.AirDefStrategy);
        cmd.Parameters.AddWithValue("@AirDefTactic", c.AirDefTactic);
        cmd.Parameters.AddWithValue("@Besieged", c.Besieged);
        cmd.Parameters.AddWithValue("@DefenseWins", c.DefenseWins);
        cmd.Parameters.AddWithValue("@CreatedAtMs", c.CreatedAtMs);
        cmd.ExecuteNonQuery();
    }

    public static Country? GetCountry(long ownerId, long chatId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = $"SELECT {COUNTRY_COLS} FROM Countries WHERE OwnerId=@id AND ChatId=@chat";
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@chat", chatId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return ReadCountry(reader);
    }

    public class EquipmentModel
    {
        public string ModelName { get; set; } = "";
        public long Count { get; set; }
    }

    public static List<EquipmentModel> GetEquipmentModels(long ownerId, long chatId, string category)
    {
        var result = new List<EquipmentModel>();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT ModelName, Count FROM EquipmentModels WHERE OwnerId=@id AND ChatId=@chat AND Category=@cat";
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@chat", chatId);
        cmd.Parameters.AddWithValue("@cat", category);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new EquipmentModel { ModelName = reader.GetString(0), Count = reader.GetInt64(1) });
        }
        return result;
    }

    public static void AddEquipmentModel(long ownerId, long chatId, string category, string modelName, long amount)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"INSERT INTO EquipmentModels(OwnerId, ChatId, Category, ModelName, Count)
                             VALUES(@id, @chat, @cat, @model, @amt)
                             ON CONFLICT(OwnerId, ChatId, Category, ModelName)
                             DO UPDATE SET Count = Count + @amt";
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@chat", chatId);
        cmd.Parameters.AddWithValue("@cat", category);
        cmd.Parameters.AddWithValue("@model", modelName);
        cmd.Parameters.AddWithValue("@amt", amount);
        cmd.ExecuteNonQuery();
    }

    public static string GetDefaultTankModel(Faction f) => f switch
    {
        Faction.USSR => "T-34",
        Faction.USA => "M4 Sherman",
        Faction.Reich => "Panzer III",
        _ => "تانک نامشخص"
    };

    public static string GetDefaultPlaneModel(Faction f) => f switch
    {
        Faction.USSR => "Yak-9",
        Faction.USA => "P-51 Mustang",
        Faction.Reich => "Bf 109",
        _ => "جنگنده نامشخص"
    };

    public static string GetDefaultBomberModel(Faction f) => f switch
    {
        Faction.USSR => "Pe-2",
        Faction.USA => "B-17",
        Faction.Reich => "Ju 88",
        _ => "بمب‌افکن نامشخص"
    };

    public static void UpdateCountryName(long ownerId, long chatId, string newName)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE Countries SET Name=@name WHERE OwnerId=@id AND ChatId=@chat";
        cmd.Parameters.AddWithValue("@name", newName);
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@chat", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateCountryFlag(long ownerId, long chatId, string flagId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE Countries SET FlagFileId=@flag WHERE OwnerId=@id AND ChatId=@chat";
        cmd.Parameters.AddWithValue("@flag", flagId);
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@chat", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateBuildingLevel(long ownerId, long chatId, string buildingType, int newLevel, long moneyDelta)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        string levelCol = buildingType switch
        {
            "factory" => "FactoryLevel",
            "port" => "PortLevel",
            "mine" => "MineLevel",
            _ => throw new ArgumentException("invalid building type")
        };
        cmd.CommandText = $"UPDATE Countries SET {levelCol} = @level, Money = Money + @delta " +
                          "WHERE OwnerId = @id AND ChatId = @chat AND Money + @delta >= 0";
        cmd.Parameters.AddWithValue("@level", newLevel);
        cmd.Parameters.AddWithValue("@delta", moneyDelta);
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@chat", chatId);
        cmd.ExecuteNonQuery();
    }

    public static List<string> GetFactionFlags(string faction)
    {
        List<string> list = new();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT FileId FROM FactionFlags WHERE Faction=@f ORDER BY Id";
        cmd.Parameters.AddWithValue("@f", faction);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(reader.GetString(0));
        return list;
    }

    public static void AddFactionFlag(string faction, string fileId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO FactionFlags(Faction,FileId) VALUES(@f,@id)";
        cmd.Parameters.AddWithValue("@f", faction);
        cmd.Parameters.AddWithValue("@id", fileId);
        cmd.ExecuteNonQuery();
    }

    public static void RemoveFactionFlag(string faction, int index)
    {
        var flags = GetFactionFlags(faction);
        if (index < 0 || index >= flags.Count) return;
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM FactionFlags WHERE rowid = (SELECT rowid FROM FactionFlags WHERE Faction=@f AND FileId=@id LIMIT 1)";
        cmd.Parameters.AddWithValue("@f", faction);
        cmd.Parameters.AddWithValue("@id", flags[index]);
        cmd.ExecuteNonQuery();
    }

    public static void SetSetting(string key, string value)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"INSERT INTO Settings(Key,Value) VALUES(@k,@v)
                            ON CONFLICT(Key) DO UPDATE SET Value=@v";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        cmd.ExecuteNonQuery();
    }

    public static string GetSetting(string key)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Value FROM Settings WHERE Key=@k";
        cmd.Parameters.AddWithValue("@k", key);
        var result = cmd.ExecuteScalar();
        return result?.ToString() ?? "";
    }

    public static void UpdateCountryResources(long ownerId, long chatId, long money, long iron, long tanks = 0)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"UPDATE Countries SET Money=@money, Iron=@iron, Tanks=@tanks
                            WHERE OwnerId=@id AND ChatId=@chat";
        cmd.Parameters.AddWithValue("@money", money);
        cmd.Parameters.AddWithValue("@tanks", tanks);
        cmd.Parameters.AddWithValue("@iron", iron);
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@chat", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void UpdatePlanesResources(long ownerId, long chatId, long money, long iron, long planes)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"UPDATE Countries SET Money=@money, Iron=@iron, Planes=@planes
                            WHERE OwnerId=@id AND ChatId=@chat";
        cmd.Parameters.AddWithValue("@money", money);
        cmd.Parameters.AddWithValue("@iron", iron);
        cmd.Parameters.AddWithValue("@planes", planes);
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@chat", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateBombersResources(long ownerId, long chatId, long money, long iron, long bombers)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"UPDATE Countries SET Money=@money, Iron=@iron, Bombers=@bombers
                            WHERE OwnerId=@id AND ChatId=@chat";
        cmd.Parameters.AddWithValue("@money", money);
        cmd.Parameters.AddWithValue("@iron", iron);
        cmd.Parameters.AddWithValue("@bombers", bombers);
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@chat", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateAntiAirResources(long ownerId, long chatId, long money, long iron, long antiair)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"UPDATE Countries SET Money=@money, Iron=@iron, AntiAir=@antiair
                            WHERE OwnerId=@id AND ChatId=@chat";
        cmd.Parameters.AddWithValue("@money", money);
        cmd.Parameters.AddWithValue("@iron", iron);
        cmd.Parameters.AddWithValue("@antiair", antiair);
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@chat", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateCountryFull(Country c)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"UPDATE Countries SET Money=@money, Iron=@iron, Population=@pop, Soldiers=@sol,
                            RecruitmentRate=@rr, Welfare=@wf, Tanks=@tanks, Planes=@planes, Bombers=@bombers, AntiAir=@antiair,
                            AirDefStrategy=@ads, AirDefTactic=@adt, Besieged=@bsg, Cities=@cities, DefenseWins=@dwins, TaxRate=@tax, DefTankPct=@dtp, DefSoldierPct=@dsp, DefFighterPct=@dfp
                            WHERE OwnerId=@id AND ChatId=@chat";
        cmd.Parameters.AddWithValue("@money", c.Money);
        cmd.Parameters.AddWithValue("@iron", c.Iron);
        cmd.Parameters.AddWithValue("@pop", c.Population);
        cmd.Parameters.AddWithValue("@sol", c.Soldiers);
        cmd.Parameters.AddWithValue("@rr", c.RecruitmentRate);
        cmd.Parameters.AddWithValue("@wf", c.Welfare);
        cmd.Parameters.AddWithValue("@tanks", c.Tanks);
        cmd.Parameters.AddWithValue("@planes", c.Planes);
        cmd.Parameters.AddWithValue("@bombers", c.Bombers);
        cmd.Parameters.AddWithValue("@antiair", c.AntiAir);
        cmd.Parameters.AddWithValue("@ads", c.AirDefStrategy);
        cmd.Parameters.AddWithValue("@adt", c.AirDefTactic);
        cmd.Parameters.AddWithValue("@bsg", c.Besieged);
        cmd.Parameters.AddWithValue("@cities", c.Cities);
        cmd.Parameters.AddWithValue("@dwins", c.DefenseWins);
        cmd.Parameters.AddWithValue("@tax", c.TaxRate);
        cmd.Parameters.AddWithValue("@dtp", c.DefTankPct);
        cmd.Parameters.AddWithValue("@dsp", c.DefSoldierPct);
        cmd.Parameters.AddWithValue("@dfp", c.DefFighterPct);
        cmd.Parameters.AddWithValue("@id", c.OwnerId);
        cmd.Parameters.AddWithValue("@chat", c.ChatId);
        cmd.ExecuteNonQuery();
    }

    public static long GetRoyalCoins(long ownerId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO RoyalCoins(OwnerId,Amount) VALUES(@id,0)";
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT Amount FROM RoyalCoins WHERE OwnerId=@id";
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public static void AddRoyalCoins(long ownerId, long amount)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO RoyalCoins(OwnerId,Amount) VALUES(@id,@amount) " +
                          "ON CONFLICT(OwnerId) DO UPDATE SET Amount=Amount+@amount";
        cmd.Parameters.AddWithValue("@id", ownerId);
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteCountry(long ownerId, long chatId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM Countries WHERE OwnerId=@oid AND ChatId=@cid";
        cmd.Parameters.AddWithValue("@oid", ownerId);
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.ExecuteNonQuery();

        using var delCd = con.CreateCommand();
        delCd.CommandText = "DELETE FROM LeaveCooldowns WHERE OwnerId=@oid AND ChatId=@cid";
        delCd.Parameters.AddWithValue("@oid", ownerId);
        delCd.Parameters.AddWithValue("@cid", chatId);
        delCd.ExecuteNonQuery();

        using var delShield = con.CreateCommand();
        delShield.CommandText = "DELETE FROM ShieldExemptions WHERE OwnerId=@oid AND ChatId=@cid";
        delShield.Parameters.AddWithValue("@oid", ownerId);
        delShield.Parameters.AddWithValue("@cid", chatId);
        delShield.ExecuteNonQuery();

        using var delDefeat = con.CreateCommand();
        delDefeat.CommandText = "DELETE FROM RoutDefeats WHERE ChatId=@cid AND (DefenderId=@oid OR AttackerId=@oid)";
        delDefeat.Parameters.AddWithValue("@oid", ownerId);
        delDefeat.Parameters.AddWithValue("@cid", chatId);
        delDefeat.ExecuteNonQuery();

        using var delAlly = con.CreateCommand();
        delAlly.CommandText = "DELETE FROM AllianceMembers WHERE AllianceId IN (SELECT Id FROM Alliances WHERE ChatId=@cid AND LeaderId=@oid); " +
                              "DELETE FROM Alliances WHERE ChatId=@cid AND LeaderId=@oid; " +
                              "DELETE FROM AllianceMembers WHERE ChatId=@cid AND UserId=@oid; " +
                              "DELETE FROM AllianceInvites WHERE ChatId=@cid AND (TargetUserId=@oid OR LeaderId=@oid); " +
                              "DELETE FROM Transfers WHERE ChatId=@cid AND (SenderId=@oid OR ReceiverId=@oid); " +
                              "DELETE FROM DeploymentContributors WHERE UserId=@oid; " +
                              "DELETE FROM Deployments WHERE ChatId=@cid AND (InitiatorId=@oid OR TargetUserId=@oid);";
        delAlly.Parameters.AddWithValue("@cid", chatId);
        delAlly.Parameters.AddWithValue("@oid", ownerId);
        delAlly.ExecuteNonQuery();
    }

    public static void SetLeaveCooldown(long ownerId, long chatId, double hours)
    {
        long until = DateTimeOffset.UtcNow.AddHours(hours).ToUnixTimeMilliseconds();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"INSERT INTO LeaveCooldowns(OwnerId,ChatId,UntilUnixMs) VALUES(@o,@c,@u)
                            ON CONFLICT(OwnerId,ChatId) DO UPDATE SET UntilUnixMs=@u";
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@c", chatId);
        cmd.Parameters.AddWithValue("@u", until);
        cmd.ExecuteNonQuery();
    }

    public static long GetLeaveCooldownRemainingMs(long ownerId, long chatId)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT UntilUnixMs FROM LeaveCooldowns WHERE OwnerId=@o AND ChatId=@c";
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@c", chatId);
        var res = cmd.ExecuteScalar();
        if (res == null || res == DBNull.Value) return 0;
        long until = Convert.ToInt64(res);
        if (until <= now)
        {
            using var del = con.CreateCommand();
            del.CommandText = "DELETE FROM LeaveCooldowns WHERE OwnerId=@o AND ChatId=@c";
            del.Parameters.AddWithValue("@o", ownerId);
            del.Parameters.AddWithValue("@c", chatId);
            del.ExecuteNonQuery();
            return 0;
        }
        return until - now;
    }

    public static void ClearLeaveCooldown(long ownerId, long chatId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM LeaveCooldowns WHERE OwnerId=@o AND ChatId=@c";
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@c", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void SetShieldExemption(long ownerId, long chatId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"INSERT OR IGNORE INTO ShieldExemptions(OwnerId,ChatId) VALUES(@o,@c)";
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@c", chatId);
        cmd.ExecuteNonQuery();
    }

    public static bool HasShieldExemption(long ownerId, long chatId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM ShieldExemptions WHERE OwnerId=@o AND ChatId=@c";
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@c", chatId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public static void ClearShieldExemption(long ownerId, long chatId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM ShieldExemptions WHERE OwnerId=@o AND ChatId=@c";
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@c", chatId);
        cmd.ExecuteNonQuery();
    }

    // ── AttackAbandonLocks ──────────────────────────────────────────
    public static void SetAttackAbandonLock(long ownerId, long durationMs)
    {
        long until = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + durationMs;
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO AttackAbandonLocks(OwnerId,LockedUntilMs) VALUES(@o,@u)";
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@u", until);
        cmd.ExecuteNonQuery();
    }
    public static bool HasAttackAbandonLock(long ownerId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT LockedUntilMs FROM AttackAbandonLocks WHERE OwnerId=@o";
        cmd.Parameters.AddWithValue("@o", ownerId);
        var val = cmd.ExecuteScalar();
        if (val == null || val == DBNull.Value) return false;
        return Convert.ToInt64(val) > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
    public static long GetAttackAbandonLockUntilMs(long ownerId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT LockedUntilMs FROM AttackAbandonLocks WHERE OwnerId=@o";
        cmd.Parameters.AddWithValue("@o", ownerId);
        var val = cmd.ExecuteScalar();
        if (val == null || val == DBNull.Value) return 0;
        return Convert.ToInt64(val);
    }
    // ── DailyDefendCounts ────────────────────────────────────────────
    public static int GetDailyDefendCount(long defenderId, string date)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Count FROM DailyDefendCounts WHERE DefenderId=@d AND AttackDate=@dt";
        cmd.Parameters.AddWithValue("@d", defenderId);
        cmd.Parameters.AddWithValue("@dt", date);
        var val = cmd.ExecuteScalar();
        return val == null || val == DBNull.Value ? 0 : Convert.ToInt32(val);
    }
    public static void IncDailyDefendCount(long defenderId, string date)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"INSERT INTO DailyDefendCounts(DefenderId,AttackDate,Count) VALUES(@d,@dt,1)
            ON CONFLICT(DefenderId,AttackDate) DO UPDATE SET Count=Count+1";
        cmd.Parameters.AddWithValue("@d", defenderId);
        cmd.Parameters.AddWithValue("@dt", date);
        cmd.ExecuteNonQuery();
    }

    // FIX(1b): AttackerFlags — ثبت حمله واقعی برای جلوگیری از فرار با حذف کشور
    public static void SetAttackerFlag(long ownerId, string date)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"INSERT OR REPLACE INTO AttackerFlags(OwnerId, AttackDate) VALUES(@o, @d)";
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@d", date);
        cmd.ExecuteNonQuery();
    }
    public static bool HasAttackerFlag(long ownerId, string date)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM AttackerFlags WHERE OwnerId=@o AND AttackDate=@d";
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@d", date);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public static int GetRoutDefeats(long defenderId, long chatId, long attackerId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Count FROM RoutDefeats WHERE DefenderId=@d AND ChatId=@c AND AttackerId=@a";
        cmd.Parameters.AddWithValue("@d", defenderId);
        cmd.Parameters.AddWithValue("@c", chatId);
        cmd.Parameters.AddWithValue("@a", attackerId);
        var res = cmd.ExecuteScalar();
        return (res == null || res == DBNull.Value) ? 0 : Convert.ToInt32(res);
    }

    public static int AddRoutDefeat(long defenderId, long chatId, long attackerId, int delta)
    {
        int cur = GetRoutDefeats(defenderId, chatId, attackerId);
        int nv = Math.Max(0, cur + delta);
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"INSERT INTO RoutDefeats(DefenderId,ChatId,AttackerId,Count) VALUES(@d,@c,@a,@n)
                            ON CONFLICT(DefenderId,ChatId,AttackerId) DO UPDATE SET Count=@n";
        cmd.Parameters.AddWithValue("@d", defenderId);
        cmd.Parameters.AddWithValue("@c", chatId);
        cmd.Parameters.AddWithValue("@a", attackerId);
        cmd.Parameters.AddWithValue("@n", nv);
        cmd.ExecuteNonQuery();
        return nv;
    }

    public static int MaxRoutDefeats(long defenderId, long chatId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(Count),0) FROM RoutDefeats WHERE DefenderId=@d AND ChatId=@c";
        cmd.Parameters.AddWithValue("@d", defenderId);
        cmd.Parameters.AddWithValue("@c", chatId);
        var res = cmd.ExecuteScalar();
        return (res == null || res == DBNull.Value) ? 0 : Convert.ToInt32(res);
    }

    public static void SetBesieged(long ownerId, long chatId, int state)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE Countries SET Besieged=@b WHERE OwnerId=@o AND ChatId=@c";
        cmd.Parameters.AddWithValue("@b", state);
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@c", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void SetCities(long ownerId, long chatId, int cities)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE Countries SET Cities=@n WHERE OwnerId=@o AND ChatId=@c";
        cmd.Parameters.AddWithValue("@n", cities);
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@c", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void SetDefenseWins(long ownerId, long chatId, int wins)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE Countries SET DefenseWins=@n WHERE OwnerId=@o AND ChatId=@c";
        cmd.Parameters.AddWithValue("@n", wins);
        cmd.Parameters.AddWithValue("@o", ownerId);
        cmd.Parameters.AddWithValue("@c", chatId);
        cmd.ExecuteNonQuery();
    }

    public const int MAX_CITIES = 20;
    public static bool AddCityToAttacker(long ownerId, long chatId)
    {
        var c = GetCountry(ownerId, chatId);
        if (c == null) return false;
        if (c.Cities >= MAX_CITIES) return false;
        SetCities(ownerId, chatId, c.Cities + 1);
        return true;
    }

    public static List<Country> GetAllCountries()
    {
        List<Country> list = new();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = $"SELECT {COUNTRY_COLS} FROM Countries";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(ReadCountry(reader));
        return list;
    }

    public static void UpdateDefense(long ownerId, long chatId, long defenseTanks, long defenseSoldiers, int strategy = 1, int tactic = 1)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"UPDATE Countries SET DefenseTanks=@dt, DefenseSoldiers=@ds, DefenseStrategy=@dstr, DefenseTactic=@dtac
                            WHERE OwnerId=@oid AND ChatId=@cid";
        cmd.Parameters.AddWithValue("@dt", defenseTanks);
        cmd.Parameters.AddWithValue("@ds", defenseSoldiers);
        cmd.Parameters.AddWithValue("@dstr", strategy);
        cmd.Parameters.AddWithValue("@dtac", tactic);
        cmd.Parameters.AddWithValue("@oid", ownerId);
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateDefenseFull(long ownerId, long chatId, long defenseTanks, long defenseSoldiers, long defenseFighters, int strategy = 1, int tactic = 1, int defTankPct = 100, int defSoldierPct = 100, int defFighterPct = 100)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"UPDATE Countries SET DefenseTanks=@dt, DefenseSoldiers=@ds, DefenseFighters=@df, DefenseStrategy=@dstr, DefenseTactic=@dtac, DefTankPct=@dtp, DefSoldierPct=@dsp, DefFighterPct=@dfp
                            WHERE OwnerId=@oid AND ChatId=@cid";
        cmd.Parameters.AddWithValue("@dt", defenseTanks);
        cmd.Parameters.AddWithValue("@ds", defenseSoldiers);
        cmd.Parameters.AddWithValue("@df", defenseFighters);
        cmd.Parameters.AddWithValue("@dstr", strategy);
        cmd.Parameters.AddWithValue("@dtac", tactic);
        cmd.Parameters.AddWithValue("@dtp", defTankPct);
        cmd.Parameters.AddWithValue("@dsp", defSoldierPct);
        cmd.Parameters.AddWithValue("@dfp", defFighterPct);
        cmd.Parameters.AddWithValue("@oid", ownerId);
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void ReconcileDefense(long ownerId, long chatId)
    {
        var c = GetCountry(ownerId, chatId);
        if (c == null) return;
        long dt = c.DefenseTanks;
        long ds = c.DefenseSoldiers;
        long df = c.DefenseFighters;
        if (c.DefTankPct > 0) dt = (long)Math.Ceiling(c.Tanks * (c.DefTankPct / 100.0));
        else if (c.Tanks > 0 && c.DefenseTanks >= c.Tanks) { c.DefTankPct = 100; dt = c.Tanks; }
        if (c.DefSoldierPct > 0) ds = (long)Math.Ceiling(c.Soldiers * (c.DefSoldierPct / 100.0));
        else if (c.Soldiers > 0 && c.DefenseSoldiers >= c.Soldiers) { c.DefSoldierPct = 100; ds = c.Soldiers; }
        if (c.DefFighterPct > 0) df = (long)Math.Ceiling(c.Planes * (c.DefFighterPct / 100.0));
        else if (c.Planes > 0 && c.DefenseFighters >= c.Planes) { c.DefFighterPct = 100; df = c.Planes; }
        long minTanks = (long)Math.Ceiling(c.Tanks * 0.2);
        long minSoldiers = (long)Math.Ceiling(c.Soldiers * 0.2);
        long minFighters = (long)Math.Ceiling(c.Planes * 0.2);
        dt = Math.Clamp(dt, minTanks, c.Tanks);
        ds = Math.Clamp(ds, minSoldiers, c.Soldiers);
        df = Math.Clamp(df, minFighters, c.Planes);
        if (dt != c.DefenseTanks || ds != c.DefenseSoldiers || df != c.DefenseFighters || c.DefTankPct == 0)
        {
            c.DefenseTanks = dt;
            c.DefenseSoldiers = ds;
            c.DefenseFighters = df;
            if (c.DefTankPct == 0) c.DefTankPct = 100;
            if (c.DefSoldierPct == 0) c.DefSoldierPct = 100;
            if (c.DefFighterPct == 0) c.DefFighterPct = 100;
            UpdateDefenseFull(ownerId, chatId, dt, ds, df, c.DefenseStrategy, c.DefenseTactic, c.DefTankPct, c.DefSoldierPct, c.DefFighterPct);
        }
    }

    public static void EnsureMinDefense(long ownerId, long chatId) => ReconcileDefense(ownerId, chatId);

    public static List<Country> GetCountriesByChatId(long chatId)
    {
        return GetAllCountries().Where(c => c.ChatId == chatId).ToList();
    }

    public static List<long> GetUserChatIds(long ownerId)
    {
        return GetAllCountries().Where(c => c.OwnerId == ownerId).Select(c => c.ChatId).Distinct().ToList();
    }

    public static List<Alliance> GetAlliancesByChatId(long chatId)
    {
        var list = new List<Alliance>();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, ChatId, Name, FlagFileId, LeaderId, CreatedAtMs FROM Alliances WHERE ChatId=@cid";
        cmd.Parameters.AddWithValue("@cid", chatId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Alliance
            {
                Id = r.GetInt64(0),
                ChatId = r.GetInt64(1),
                Name = r.GetString(2),
                FlagFileId = r.IsDBNull(3) ? "" : r.GetString(3),
                LeaderId = r.GetInt64(4),
                CreatedAtMs = r.GetInt64(5)
            });
        }
        return list;
    }

    public static Alliance? GetAllianceById(long allianceId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, ChatId, Name, FlagFileId, LeaderId, CreatedAtMs FROM Alliances WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", allianceId);
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            return new Alliance
            {
                Id = r.GetInt64(0),
                ChatId = r.GetInt64(1),
                Name = r.GetString(2),
                FlagFileId = r.IsDBNull(3) ? "" : r.GetString(3),
                LeaderId = r.GetInt64(4),
                CreatedAtMs = r.GetInt64(5)
            };
        }
        return null;
    }

    public static bool AllianceNameExists(long chatId, string name)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM Alliances WHERE ChatId=@cid AND LOWER(Name)=LOWER(@name) LIMIT 1";
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.Parameters.AddWithValue("@name", name.Trim());
        using var r = cmd.ExecuteReader();
        return r.Read();
    }

    public static long GetUserAllianceId(long chatId, long userId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT AllianceId FROM AllianceMembers WHERE ChatId=@cid AND UserId=@uid LIMIT 1";
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.Parameters.AddWithValue("@uid", userId);
        var val = cmd.ExecuteScalar();
        return val != null && val != DBNull.Value ? Convert.ToInt64(val) : 0;
    }

    public static List<long> GetAllianceMembers(long allianceId)
    {
        var list = new List<long>();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT UserId FROM AllianceMembers WHERE AllianceId=@aid";
        cmd.Parameters.AddWithValue("@aid", allianceId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(r.GetInt64(0));
        return list;
    }

    public static long AddAlliance(Alliance a)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO Alliances(ChatId, Name, FlagFileId, LeaderId, CreatedAtMs) VALUES(@cid, @name, @flag, @lid, @ms); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@cid", a.ChatId);
        cmd.Parameters.AddWithValue("@name", a.Name);
        cmd.Parameters.AddWithValue("@flag", a.FlagFileId);
        cmd.Parameters.AddWithValue("@lid", a.LeaderId);
        cmd.Parameters.AddWithValue("@ms", a.CreatedAtMs);
        long aid = Convert.ToInt64(cmd.ExecuteScalar());
        using var cmd2 = con.CreateCommand();
        cmd2.CommandText = "INSERT OR REPLACE INTO AllianceMembers(AllianceId, ChatId, UserId) VALUES(@aid, @cid, @uid)";
        cmd2.Parameters.AddWithValue("@aid", aid);
        cmd2.Parameters.AddWithValue("@cid", a.ChatId);
        cmd2.Parameters.AddWithValue("@uid", a.LeaderId);
        cmd2.ExecuteNonQuery();
        return aid;
    }

    public static void AddAllianceMember(long allianceId, long chatId, long userId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO AllianceMembers(AllianceId, ChatId, UserId) VALUES(@aid, @cid, @uid)";
        cmd.Parameters.AddWithValue("@aid", allianceId);
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.ExecuteNonQuery();
    }

    public static void RemoveAllianceMember(long allianceId, long chatId, long userId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM AllianceMembers WHERE ChatId=@cid AND UserId=@uid";
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteAlliance(long allianceId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM AllianceMembers WHERE AllianceId=@aid; DELETE FROM Alliances WHERE Id=@aid; DELETE FROM AllianceInvites WHERE AllianceId=@aid;";
        cmd.Parameters.AddWithValue("@aid", allianceId);
        cmd.ExecuteNonQuery();
    }

    public static long AddAllianceInvite(AllianceInvite inv)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO AllianceInvites(AllianceId, ChatId, TargetUserId, LeaderId, CreatedAtMs) VALUES(@aid, @cid, @tuid, @lid, @ms); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@aid", inv.AllianceId);
        cmd.Parameters.AddWithValue("@cid", inv.ChatId);
        cmd.Parameters.AddWithValue("@tuid", inv.TargetUserId);
        cmd.Parameters.AddWithValue("@lid", inv.LeaderId);
        cmd.Parameters.AddWithValue("@ms", inv.CreatedAtMs);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public static AllianceInvite? GetAllianceInvite(long inviteId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, AllianceId, ChatId, TargetUserId, LeaderId, CreatedAtMs FROM AllianceInvites WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", inviteId);
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            return new AllianceInvite
            {
                Id = r.GetInt64(0),
                AllianceId = r.GetInt64(1),
                ChatId = r.GetInt64(2),
                TargetUserId = r.GetInt64(3),
                LeaderId = r.GetInt64(4),
                CreatedAtMs = r.GetInt64(5)
            };
        }
        return null;
    }

    public static void DeleteAllianceInvite(long inviteId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM AllianceInvites WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", inviteId);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteUserInvites(long chatId, long targetUserId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM AllianceInvites WHERE ChatId=@cid AND TargetUserId=@uid";
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.Parameters.AddWithValue("@uid", targetUserId);
        cmd.ExecuteNonQuery();
    }

    public static long AddTransfer(Transfer t)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"INSERT INTO Transfers(ChatId, AllianceId, SenderId, ReceiverId, ResourceType, Amount, ArriveAtMs, Notified)
                            VALUES(@cid, @aid, @sid, @rid, @res, @amt, @ms, @notif); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@cid", t.ChatId);
        cmd.Parameters.AddWithValue("@aid", t.AllianceId);
        cmd.Parameters.AddWithValue("@sid", t.SenderId);
        cmd.Parameters.AddWithValue("@rid", t.ReceiverId);
        cmd.Parameters.AddWithValue("@res", t.ResourceType);
        cmd.Parameters.AddWithValue("@amt", t.Amount);
        cmd.Parameters.AddWithValue("@ms", t.ArriveAtMs);
        cmd.Parameters.AddWithValue("@notif", t.Notified);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public static List<Transfer> GetActiveTransfers()
    {
        var list = new List<Transfer>();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, ChatId, AllianceId, SenderId, ReceiverId, ResourceType, Amount, ArriveAtMs, Notified FROM Transfers";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Transfer
            {
                Id = r.GetInt64(0),
                ChatId = r.GetInt64(1),
                AllianceId = r.GetInt64(2),
                SenderId = r.GetInt64(3),
                ReceiverId = r.GetInt64(4),
                ResourceType = r.GetString(5),
                Amount = r.GetInt64(6),
                ArriveAtMs = r.GetInt64(7),
                Notified = r.GetInt32(8)
            });
        }
        return list;
    }

    public static void UpdateTransferNotified(long id, int notified = 1)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE Transfers SET Notified=@n WHERE Id=@id";
        cmd.Parameters.AddWithValue("@n", notified);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteTransfer(long id)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM Transfers WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public static long AddDeployment(Deployment d)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"INSERT INTO Deployments(ChatId, AllianceId, InitiatorId, TargetUserId, Type, DurationHours, FormationType, Strategy, Tactic, Tanks, Soldiers, Fighters, Bombers, CreatedAtMs, EndAtMs, LastWarnMs, AnnounceMsgId)
                            VALUES(@cid, @aid, @iid, @tid, @type, @dur, @form, @str, @tac, @tnk, @sol, @fig, @bom, @cms, @ems, @lms, @amid); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@cid", d.ChatId);
        cmd.Parameters.AddWithValue("@aid", d.AllianceId);
        cmd.Parameters.AddWithValue("@iid", d.InitiatorId);
        cmd.Parameters.AddWithValue("@tid", d.TargetUserId);
        cmd.Parameters.AddWithValue("@type", d.Type);
        cmd.Parameters.AddWithValue("@dur", d.DurationHours);
        cmd.Parameters.AddWithValue("@form", d.FormationType);
        cmd.Parameters.AddWithValue("@str", d.Strategy);
        cmd.Parameters.AddWithValue("@tac", d.Tactic);
        cmd.Parameters.AddWithValue("@tnk", d.Tanks);
        cmd.Parameters.AddWithValue("@sol", d.Soldiers);
        cmd.Parameters.AddWithValue("@fig", d.Fighters);
        cmd.Parameters.AddWithValue("@bom", d.Bombers);
        cmd.Parameters.AddWithValue("@cms", d.CreatedAtMs);
        cmd.Parameters.AddWithValue("@ems", d.EndAtMs);
        cmd.Parameters.AddWithValue("@lms", d.LastWarnMs);
        cmd.Parameters.AddWithValue("@amid", d.AnnounceMsgId);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    // FIX(2): ذخیرهٔ MessageId پیام پین‌شدهٔ صف‌آرایی
    public static void UpdateDeploymentAnnounceMsg(long id, int msgId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE Deployments SET AnnounceMsgId=@m WHERE Id=@id";
        cmd.Parameters.AddWithValue("@m", msgId);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public static List<Deployment> GetActiveDeployments()
    {
        var list = new List<Deployment>();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, ChatId, AllianceId, InitiatorId, TargetUserId, Type, DurationHours, FormationType, Strategy, Tactic, Tanks, Soldiers, Fighters, Bombers, CreatedAtMs, EndAtMs, LastWarnMs, AnnounceMsgId FROM Deployments";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Deployment
            {
                Id = r.GetInt64(0),
                ChatId = r.GetInt64(1),
                AllianceId = r.GetInt64(2),
                InitiatorId = r.GetInt64(3),
                TargetUserId = r.GetInt64(4),
                Type = r.GetString(5),
                DurationHours = r.GetInt32(6),
                FormationType = r.GetString(7),
                Strategy = r.GetInt32(8),
                Tactic = r.GetInt32(9),
                Tanks = r.GetInt64(10),
                Soldiers = r.GetInt64(11),
                Fighters = r.GetInt64(12),
                Bombers = r.GetInt64(13),
                CreatedAtMs = r.GetInt64(14),
                EndAtMs = r.GetInt64(15),
                LastWarnMs = r.GetInt64(16),
                AnnounceMsgId = r.IsDBNull(17) ? 0 : r.GetInt32(17)
            });
        }
        return list;
    }

    public static int GetRecentAllianceDeploymentsCount(long allianceId, long sinceMs)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Deployments WHERE AllianceId=@aid AND CreatedAtMs>=@ms";
        cmd.Parameters.AddWithValue("@aid", allianceId);
        cmd.Parameters.AddWithValue("@ms", sinceMs);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static bool HasRecentTargetDeployment(long chatId, long targetUserId, long sinceMs)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM Deployments WHERE ChatId=@cid AND TargetUserId=@tid AND CreatedAtMs>=@ms LIMIT 1";
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.Parameters.AddWithValue("@tid", targetUserId);
        cmd.Parameters.AddWithValue("@ms", sinceMs);
        using var r = cmd.ExecuteReader();
        return r.Read();
    }

    public static void UpdateDeploymentWarnMs(long id, long warnMs)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE Deployments SET LastWarnMs=@ms WHERE Id=@id";
        cmd.Parameters.AddWithValue("@ms", warnMs);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteDeployment(long id)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM DeploymentContributors WHERE DeploymentId=@id; DELETE FROM Deployments WHERE Id=@id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateDeploymentEndMs(long id, long endMs)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE Deployments SET EndAtMs=@ms WHERE Id=@id";
        cmd.Parameters.AddWithValue("@ms", endMs);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public static void CancelDeploymentForces(Deployment d)
    {
        var contribs = GetDeploymentContributors(d.Id);
        if (d.Type == "Defensive")
        {
            var tcDef = GetCountry(d.TargetUserId, d.ChatId);
            if (tcDef != null)
            {
                tcDef.Tanks = Math.Max(0, tcDef.Tanks - d.Tanks);
                tcDef.Soldiers = Math.Max(0, tcDef.Soldiers - d.Soldiers);
                tcDef.Planes = Math.Max(0, tcDef.Planes - d.Fighters);
                tcDef.Bombers = Math.Max(0, tcDef.Bombers - d.Bombers);
                tcDef.DefenseTanks = Math.Max(0, tcDef.DefenseTanks - d.Tanks);
                tcDef.DefenseSoldiers = Math.Max(0, tcDef.DefenseSoldiers - d.Soldiers);
                tcDef.DefenseFighters = Math.Max(0, tcDef.DefenseFighters - d.Fighters);
                UpdateCountryFull(tcDef);
                ReconcileDefense(tcDef.OwnerId, tcDef.ChatId);
            }
        }
        foreach (var c in contribs)
        {
            var cc = GetCountry(c.UserId, d.ChatId);
            if (cc != null)
            {
                cc.Tanks += c.Tanks;
                cc.Soldiers += c.Soldiers;
                cc.Planes += c.Fighters;
                cc.Bombers += c.Bombers;
                UpdateCountryFull(cc);
                ReconcileDefense(c.UserId, d.ChatId);
            }
        }
        DeleteDeployment(d.Id);
    }

    public static Deployment? GetDeploymentById(long id)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, ChatId, AllianceId, InitiatorId, TargetUserId, Type, DurationHours, FormationType, Strategy, Tactic, Tanks, Soldiers, Fighters, Bombers, CreatedAtMs, EndAtMs, LastWarnMs, AnnounceMsgId FROM Deployments WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            return new Deployment
            {
                Id = r.GetInt64(0), ChatId = r.GetInt64(1), AllianceId = r.GetInt64(2), InitiatorId = r.GetInt64(3), TargetUserId = r.GetInt64(4), Type = r.GetString(5), DurationHours = r.GetInt32(6), FormationType = r.GetString(7), Strategy = r.GetInt32(8), Tactic = r.GetInt32(9), Tanks = r.GetInt64(10), Soldiers = r.GetInt64(11), Fighters = r.GetInt64(12), Bombers = r.GetInt64(13), CreatedAtMs = r.GetInt64(14), EndAtMs = r.GetInt64(15), LastWarnMs = r.GetInt64(16), AnnounceMsgId = r.IsDBNull(17) ? 0 : r.GetInt32(17)
            };
        }
        return null;
    }

    public static void UpdateDeploymentForces(Deployment d)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE Deployments SET Tanks=@t, Soldiers=@s, Fighters=@f, Bombers=@b WHERE Id=@id";
        cmd.Parameters.AddWithValue("@t", d.Tanks);
        cmd.Parameters.AddWithValue("@s", d.Soldiers);
        cmd.Parameters.AddWithValue("@f", d.Fighters);
        cmd.Parameters.AddWithValue("@b", d.Bombers);
        cmd.Parameters.AddWithValue("@id", d.Id);
        cmd.ExecuteNonQuery();
    }

    public static void AddDeploymentContributor(DeploymentContributor c)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO DeploymentContributors(DeploymentId, UserId, Tanks, Soldiers, Fighters, Bombers, Strategy, Tactic) VALUES(@did, @uid, @t, @s, @f, @b, @str, @tac)";
        cmd.Parameters.AddWithValue("@did", c.DeploymentId);
        cmd.Parameters.AddWithValue("@uid", c.UserId);
        cmd.Parameters.AddWithValue("@t", c.Tanks);
        cmd.Parameters.AddWithValue("@s", c.Soldiers);
        cmd.Parameters.AddWithValue("@f", c.Fighters);
        cmd.Parameters.AddWithValue("@b", c.Bombers);
        cmd.Parameters.AddWithValue("@str", c.Strategy);
        cmd.Parameters.AddWithValue("@tac", c.Tactic);
        cmd.ExecuteNonQuery();
    }

    public static List<DeploymentContributor> GetDeploymentContributors(long depId)
    {
        var list = new List<DeploymentContributor>();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, DeploymentId, UserId, Tanks, Soldiers, Fighters, Bombers, Strategy, Tactic FROM DeploymentContributors WHERE DeploymentId=@did";
        cmd.Parameters.AddWithValue("@did", depId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new DeploymentContributor
            {
                Id = r.GetInt64(0), DeploymentId = r.GetInt64(1), UserId = r.GetInt64(2), Tanks = r.GetInt64(3), Soldiers = r.GetInt64(4), Fighters = r.GetInt64(5), Bombers = r.GetInt64(6), Strategy = r.GetInt32(7), Tactic = r.GetInt32(8)
            });
        }
        return list;
    }
    public static void DeleteDeploymentContributorById(long contribId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM DeploymentContributors WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", contribId);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteDeploymentContributorsByUser(long depId, long userId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM DeploymentContributors WHERE DeploymentId=@did AND UserId=@uid";
        cmd.Parameters.AddWithValue("@did", depId);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.ExecuteNonQuery();
    }
    public static long AddVisionLog(long sourceChatId, long sourceUserId, long destChatId, int isUserMode)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO VisionLogs(SourceChatId, SourceUserId, DestChatId, IsUserMode, CreatedAtMs) VALUES(@sc, @su, @dc, @mode, @ms); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@sc", sourceChatId);
        cmd.Parameters.AddWithValue("@su", sourceUserId);
        cmd.Parameters.AddWithValue("@dc", destChatId);
        cmd.Parameters.AddWithValue("@mode", isUserMode);
        cmd.Parameters.AddWithValue("@ms", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        return Convert.ToInt64(cmd.ExecuteScalar());
    }
    public static List<(long Id, long SourceChatId, long SourceUserId, long DestChatId, int IsUserMode)> GetVisionLogsBySourceChat(long chatId)
    {
        var list = new List<(long, long, long, long, int)>();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, SourceChatId, SourceUserId, DestChatId, IsUserMode FROM VisionLogs WHERE SourceChatId=@cid";
        cmd.Parameters.AddWithValue("@cid", chatId);
        using var r = cmd.ExecuteReader();
        while(r.Read()){ list.Add((r.GetInt64(0), r.GetInt64(1), r.GetInt64(2), r.GetInt64(3), r.GetInt32(4))); }
        return list;
    }
    public static List<(long Id, long SourceChatId, long SourceUserId, long DestChatId, int IsUserMode)> GetVisionLogsBySourceUser(long userId)
    {
        var list = new List<(long, long, long, long, int)>();
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Id, SourceChatId, SourceUserId, DestChatId, IsUserMode FROM VisionLogs WHERE SourceUserId=@uid";
        cmd.Parameters.AddWithValue("@uid", userId);
        using var r = cmd.ExecuteReader();
        while(r.Read()){ list.Add((r.GetInt64(0), r.GetInt64(1), r.GetInt64(2), r.GetInt64(3), r.GetInt32(4))); }
        return list;
    }
    public static void AddVisionMessageMap(long srcChat, long srcMsg, long srcUser, long dstChat, long dstMsg)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT INTO VisionMessageMap(SourceChatId, SourceMessageId, SourceUserId, DestChatId, DestMessageId, CreatedAtMs) VALUES(@sc, @sm, @su, @dc, @dm, @ms)";
        cmd.Parameters.AddWithValue("@sc", srcChat);
        cmd.Parameters.AddWithValue("@sm", srcMsg);
        cmd.Parameters.AddWithValue("@su", srcUser);
        cmd.Parameters.AddWithValue("@dc", dstChat);
        cmd.Parameters.AddWithValue("@dm", dstMsg);
        cmd.Parameters.AddWithValue("@ms", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        cmd.ExecuteNonQuery();
    }
    public static (long DestChatId, long DestMessageId, long SourceUserId)? GetDestMessageId(long srcChat, long srcMsg, long dstChat)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT DestChatId, DestMessageId, SourceUserId FROM VisionMessageMap WHERE SourceChatId=@sc AND SourceMessageId=@sm AND DestChatId=@dc LIMIT 1";
        cmd.Parameters.AddWithValue("@sc", srcChat);
        cmd.Parameters.AddWithValue("@sm", srcMsg);
        cmd.Parameters.AddWithValue("@dc", dstChat);
        using var r = cmd.ExecuteReader();
        if(r.Read()) return (r.GetInt64(0), r.GetInt64(1), r.GetInt64(2));
        return null;
    }
    public static (long SourceChatId, long SourceMessageId, long SourceUserId)? GetSourceByDestId(long dstChat, long dstMsg)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT SourceChatId, SourceMessageId, SourceUserId FROM VisionMessageMap WHERE DestChatId=@dc AND DestMessageId=@dm LIMIT 1";
        cmd.Parameters.AddWithValue("@dc", dstChat);
        cmd.Parameters.AddWithValue("@dm", dstMsg);
        using var r = cmd.ExecuteReader();
        if(r.Read()) return (r.GetInt64(0), r.GetInt64(1), r.GetInt64(2));
        return null;
    }



    public static void SetGroupLockExemption(long chatId, bool exempt)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        if (exempt)
            cmd.CommandText = "INSERT OR IGNORE INTO GroupLockExemptions(ChatId) VALUES(@cid)";
        else
            cmd.CommandText = "DELETE FROM GroupLockExemptions WHERE ChatId=@cid";
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.ExecuteNonQuery();
    }

    public static bool HasGroupLockExemption(long chatId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM GroupLockExemptions WHERE ChatId=@cid LIMIT 1";
        cmd.Parameters.AddWithValue("@cid", chatId);
        using var r = cmd.ExecuteReader();
        return r.Read();
    }

    public static void ClearAllLeaveCooldownsInChat(long chatId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM LeaveCooldowns WHERE ChatId=@cid";
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.ExecuteNonQuery();
    }

    public static void SetAllShieldExemptionsInChat(long chatId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO ShieldExemptions(OwnerId, ChatId) SELECT OwnerId, ChatId FROM Countries WHERE ChatId=@cid";
        cmd.Parameters.AddWithValue("@cid", chatId);
        cmd.ExecuteNonQuery();
    }
}

// ============================================================
//  BattleResult & WarEngine — موتور جنگ (در فایل WarEngine.cs)
// ============================================================
partial class Program
{
    const string BOT_TOKEN = "8604968567:AAF7qWHrA0jv3_4edAFhPnz2uANQURbBkos";
    const long OWNER_ID = 8248899977L;
    static TelegramBotClient bot = null!;
    static readonly ConcurrentDictionary<long, UserSession> sessions = new();
    static readonly Random rng = new();
    static readonly ConcurrentDictionary<long, SemaphoreSlim> userLocks = new();
    static readonly HashSet<int> processedUpdates = new();
    static readonly object processedLock = new();

    sealed class MsgContext { public long UserId; public long ChatId; public int MessageId; public bool Marked; }
    static readonly AsyncLocal<MsgContext?> incomingCtx = new();

    static void MarkIncomingHandled()
    {
        var c = incomingCtx.Value;
        if (c != null && !c.Marked)
        {
            c.Marked = true;
            Database.MarkPlayerActive(c.UserId);
            ScheduleDelete(c.ChatId, c.MessageId, 30);
        }
    }

    static Timer? assetUpdateTimer;
    static Timer? transferTimer;
    static int assetUpdateRunning = 0;
    static DateTime lastAssetRunUtc = DateTime.MinValue;

    static readonly ConcurrentDictionary<string, int> attackCounts = new();
    static int MAX_ATTACKS_PER_UPDATE = 8;
    static readonly ConcurrentDictionary<string, int> transferCounts = new();
    static int MAX_TRANSFERS_PER_UPDATE = 2;
    static DateTime lastAssetUpdateAt = DateTime.MinValue;
    static int ATTACK_LOCK_MINUTES = 30;
    static double SHIELD_HOURS = 48.0;

    static string AtkKey(long chatId, long ownerId) => $"{chatId}:{ownerId}";
    static int GetAttackCount(long chatId, long ownerId) => attackCounts.TryGetValue(AtkKey(chatId, ownerId), out var v) ? v : 0;
    static int IncAttackCount(long chatId, long ownerId) => attackCounts.AddOrUpdate(AtkKey(chatId, ownerId), 1, (_, v) => v + 1);
    static string TfKey(long chatId, long ownerId) => $"{chatId}:{ownerId}";
    static int GetTransferCount(long chatId, long ownerId) => transferCounts.TryGetValue(TfKey(chatId, ownerId), out var v) ? v : 0;
    static int IncTransferCount(long chatId, long ownerId) => transferCounts.AddOrUpdate(TfKey(chatId, ownerId), 1, (_, v) => v + 1);

    static string UpdateMode = "daily";
    static int UpdateValue = 1200;
    static string SpecialPhotoFileId = "";

    static readonly HashSet<long> KnownGroups = new();

    static readonly int[] FactoryUpgradeCost = { 0, 5, 12, 30, 80 };
    static readonly int[] PortUpgradeCost = { 0, 13, 25, 50, 75 };
    static readonly int[] MineUpgradeCost = { 0, 5, 12, 30, 80, 0, 0 };
    static readonly double[] FactoryIncome = { 0, 1, 2, 5, 15, 30 };
    static readonly double[] PortIncome = { 0, 1, 2, 4, 8, 15 };
    static readonly double[] MineIncome = { 0, 1, 2, 5, 15, 30, 40, 50 };

    const string MsgNoCountryGuide = "❌ شما در این گپ کشوری ندارید.\nℹ️ با نوشتن دستور «راهنما» می‌توانید راهنمای بازی را ببینید. دستور دریافت کشور «انتخاب کشور» است.";

    // ============================================================
    //  متن راهنمای کامل — FIX(4)
    //  در گروه و پیوی یکسان استفاده می‌شود.
    // ============================================================
    const string HelpText =
        "📘 <b>راهنمای کامل آلیس</b>\n" +
        "برای اجرای هر بخش، فقط کافی است دستور مربوطه را (در گروه) بنویسید.\n" +
        "بعضی بخش‌ها (حمله، ترنسفر، صف‌آرایی، وضعیت دفاع) برای تنظیم دقیق به <b>پیوی ربات</b> منتقل می‌شوند.\n" +
        "برای لغو هر عملیات نیمه‌کاره، کلمهٔ «<b>لغو</b>» را بنویسید.\n" +
        "──────────────\n\n" +

        "🌍 <b>شروع و مدیریت کشور</b>\n" +
        "• <b>انتخاب کشور</b> — ساخت کشور جدید (انتخاب فکشن + نام).\n" +
        "• <b>دارایی</b> (یا «کشورم») — مشاهدهٔ کامل وضعیت اقتصادی و نظامی کشور.\n" +
        "• <b>مان پاور</b> — قدرت کل کشور و تفکیک عوامل مؤثر بر آن.\n" +
        "• <b>تغییر اسم</b> — تغییر نام کشور.\n" +
        "• <b>تغییر پرچم</b> — ارسال عکس برای پرچم جدید.\n" +
        "• <b>انصراف</b> — حذف کامل کشور در این گپ (۲۴ ساعت قفل ساخت مجدد).\n\n" +

        "🏗 <b>اقتصاد و توسعه</b>\n" +
        "• <b>اقتصاد</b> (یا «ساختمان») — ارتقای 🏭 کارخانه، ⚓ بندر و ⛏️ معدن برای افزایش درآمد.\n" +
        "• <b>مالیات</b> — تنظیم نرخ مالیات (۰ تا ۱۰۰٪). مالیات بالاتر = درآمد بیشتر ولی رفاه کمتر.\n" +
        "• <b>آموزش سرباز</b> — تنظیم نرخ سربازگیری (۰ تا ۱۰). نرخ بالاتر = سرباز بیشتر ولی رفاه کمتر.\n" +
        "• <b>ترید</b> — تبدیل رویال‌کوین به پول (هر ۱ رویال = ۱۰K پول).\n\n" +

        "⚔️ <b>ساخت ارتش</b>\n" +
        "• <b>ساخت تانک</b> — خرید تانک (نیاز به پول + آهن).\n" +
        "• <b>ساخت هواپیما</b> — خرید جنگنده و بمب‌افکن.\n" +
        "• <b>ساخت بمب افکن</b> — خرید اختصاصی بمب‌افکن.\n" +
        "• <b>پدافند</b> — خرید توپ ضدهوایی برای دفاع در برابر حملهٔ هوایی.\n\n" +

        "🛡 <b>دفاع</b>\n" +
        "• <b>وضعیت دفاع</b> — مشاهده و تنظیم نیروی دفاعی، استراتژی/تاکتیک زمینی و دفاع هوایی (در پیوی).\n" +
        "  حداقل ۲۰٪ از هر نیرو همیشه به دفاع اختصاص می‌یابد.\n\n" +

        "🗡 <b>حمله</b>\n" +
        "• <b>حمله</b> — انتخاب هدف در پیوی، سپس استراتژی، تاکتیک و تعداد نیروی زمینی/هوایی.\n" +
        "  ⏳ پس از هر آپدیت دارایی، حمله تا ۳۰ دقیقه قفل است.\n" +
        "  🛡 کشورهای تازه‌ساخت تا ۴۸ ساعت سپر دارند و قابل حمله نیستند.\n\n" +

        "🤝 <b>اتحادها</b>\n" +
        "• <b>ساخت اتحاد</b> — تاسیس اتحاد (نام + پرچم). شما رهبر می‌شوید.\n" +
        "• <b>ایجاد درخواست عضویت</b> — روی پیام بازیکن ریپلای کنید تا دعوت شود (فقط رهبر).\n" +
        "• <b>وضعیت اتحاد</b> — رده‌بندی اعضا و مان‌پاور اتحاد.\n" +
        "• <b>لیست اتحاد ها</b> — همهٔ اتحادهای گروه بر اساس قدرت.\n" +
        "• <b>حذف N</b> — اخراج عضو شمارهٔ N (فقط رهبر).\n" +
        "• <b>خروج از اتحاد</b> — خروج عضو عادی.\n" +
        "• <b>انحلال اتحاد</b> — انحلال کامل توسط رهبر.\n\n" +

        "🚚 <b>عملیات مشترک اتحاد</b>\n" +
        "• <b>ترنسفر</b> — ارسال پول/آهن/سرباز/تانک/جنگنده/بمب‌افکن به هم‌اتحادی‌ها (در پیوی).\n" +
        "• <b>صف آرایی تهاجمی</b> — اعلام حملهٔ گروهی اتحاد علیه یک کشور بیرونی.\n" +
        "• <b>صف آرایی دفاعی</b> — تشکیل خط دفاعی مشترک برای یک هم‌اتحادی.\n" +
        "• <b>اعزام نیرو</b> — پیوستن و فرستادن نیرو به یک صف‌آرایی فعال اتحاد (یا روی دکمهٔ «⚔️ مشارکت و اعزام نیرو» زیر پیام صف‌آرایی بزنید).\n" +
        "• <b>لغو صف آرایی</b> — لغو صف‌آرایی توسط سازنده یا رهبر (پیام پین‌شده هم برداشته و حذف می‌شود).\n\n" +

        "ℹ️ <b>نکات</b>\n" +
        "• همهٔ دستورها در گروه اجرا می‌شوند؛ ربات در پیوی فقط ادامهٔ عملیات و همین راهنما را انجام می‌دهد.\n" +
        "• قبل از استفاده از حمله/ترنسفر/صف‌آرایی، حتماً یک‌بار ربات را در پیوی <b>استارت</b> کنید.\n" +
        "──────────────\n📢 @alice_safe_house1";

    // ============================================================
    //  منطقه‌زمانی تهران — مقاوم و مستقل از تنظیمات سرور
    // ============================================================
    static readonly TimeSpan TehranOffset = TimeSpan.FromHours(3.5);
    static DateTime GetTehranNow()
    {
        return DateTime.UtcNow.AddHours(3.5);
    }

    static async Task Main()
    {
        Database.Init();
        Database.InitActivity();
        Database.InitAdminPanel(OWNER_ID);
        LoadSettings();
        bot = new TelegramBotClient(BOT_TOKEN);
        Console.WriteLine("Bot starting...");
        using var cts = new CancellationTokenSource();
        bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: cts.Token
        );
        Console.WriteLine("Bot is running...");
        StartAssetUpdateTimer();
        StartTransferTimer();
        StartActivityStatsTimer();
        await Task.Delay(-1);
    }

    static void LoadSettings()
    {
        var mode = Database.GetSetting("UpdateMode");
        var val = Database.GetSetting("UpdateValue");
        var special = Database.GetSetting("SpecialPhotoFileId");
        if (!string.IsNullOrEmpty(mode)) UpdateMode = mode;
        if (TryParseInt(val, out int v)) UpdateValue = v;
        if (!string.IsNullOrEmpty(special)) SpecialPhotoFileId = special;
    }

    static string NormalizeDigits(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s.Trim())
        {
            if (ch >= '\u06F0' && ch <= '\u06F9') sb.Append((char)('0' + (ch - '\u06F0')));
            else if (ch >= '\u0660' && ch <= '\u0669') sb.Append((char)('0' + (ch - '\u0660')));
            else if (ch == '\u066C' || ch == ',' || ch == '\u060C' || ch == ' ' || ch == '\u200c') { }
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    static bool TryParseLong(string? s, out long v) =>
        long.TryParse(NormalizeDigits(s), NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
    static bool TryParseInt(string? s, out int v) =>
        int.TryParse(NormalizeDigits(s), NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
    static string InventoryLine(long amount) =>
        amount > 0 ? $"موجودی: {amount:N0}" : "⚠️ موجودی نداری";

    static void ScheduleDelete(long chatId, int messageId, int seconds = 30)
    {
        if (messageId == 0) return;
        if (chatId == OWNER_ID) return;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds));
                await bot.DeleteMessageAsync(chatId, messageId);
            }
            catch { }
        });
    }

    static void DeleteNow(long chatId, int messageId)
    {
        if (messageId == 0) return;
        _ = Task.Run(async () =>
        {
            try { await bot.DeleteMessageAsync(chatId, messageId); } catch { }
        });
    }

    // FIX(2): آنپین + حذف پیام اعلام صف‌آرایی
    static async Task UnpinAndDeleteAnnounce(long chatId, int messageId, CancellationToken ct = default)
    {
        if (messageId == 0) return;
        try { await bot.UnpinChatMessageAsync(chatId, messageId, cancellationToken: ct); } catch { }
        try { await bot.DeleteMessageAsync(chatId, messageId, cancellationToken: ct); } catch { }
    }

    static async Task<Message> SendTemp(long chatId, string text, IReplyMarkup? markup = null,
        int? replyTo = null, ParseMode? parseMode = null, CancellationToken ct = default)
    {
        MarkIncomingHandled();
        var m = await bot.SendTextMessageAsync(chatId, text, parseMode: parseMode,
            replyToMessageId: replyTo, replyMarkup: markup, cancellationToken: ct);
        ScheduleDelete(chatId, m.MessageId, 30);
        return m;
    }

    static async Task<Message> SendTempPhoto(long chatId, string fileId, string caption,
        IReplyMarkup? markup = null, ParseMode? parseMode = null, CancellationToken ct = default)
    {
        MarkIncomingHandled();
        Message m;
        try
        {
            m = await bot.SendPhotoAsync(chatId, fileId, caption: caption, parseMode: parseMode,
                replyMarkup: markup, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PHOTO FALLBACK] {ex.Message}");
            m = await bot.SendTextMessageAsync(chatId, caption, parseMode: parseMode,
                replyMarkup: markup, cancellationToken: ct);
        }
        ScheduleDelete(chatId, m.MessageId, 30);
        return m;
    }

    static async Task<Message> SendPermanent(long chatId, string text, IReplyMarkup? markup = null,
        int? replyTo = null, ParseMode? parseMode = null, CancellationToken ct = default)
    {
        MarkIncomingHandled();
        return await bot.SendTextMessageAsync(chatId, text, parseMode: parseMode,
            replyToMessageId: replyTo, replyMarkup: markup, cancellationToken: ct);
    }

    static async Task<Message> SendPermanentPhoto(long chatId, string fileId, string caption,
        IReplyMarkup? markup = null, ParseMode? parseMode = null, CancellationToken ct = default)
    {
        MarkIncomingHandled();
        try
        {
            return await bot.SendPhotoAsync(chatId, fileId, caption: caption, parseMode: parseMode,
                replyMarkup: markup, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PHOTO FALLBACK] {ex.Message}");
            return await bot.SendTextMessageAsync(chatId, caption, parseMode: parseMode,
                replyMarkup: markup, cancellationToken: ct);
        }
    }

    static async Task<Message> SendPrompt(long uid, long chatId, string text, IReplyMarkup? markup = null, CancellationToken ct = default)
    {
        MarkIncomingHandled();
        ClearPromptNow(uid);
        var m = await bot.SendTextMessageAsync(chatId, text, replyMarkup: markup, cancellationToken: ct);
        if (sessions.TryGetValue(uid, out var s))
        {
            s.PromptChatId = chatId;
            s.PromptMsgId = m.MessageId;
        }
        return m;
    }

    static void TrackPrompt(long uid, long chatId, int messageId)
    {
        if (sessions.TryGetValue(uid, out var s))
        {
            s.PromptChatId = chatId;
            s.PromptMsgId = messageId;
        }
    }

    static void ClearPromptNow(long uid)
    {
        if (sessions.TryGetValue(uid, out var s) && s.PromptMsgId != 0)
        {
            DeleteNow(s.PromptChatId, s.PromptMsgId);
            s.PromptMsgId = 0;
        }
    }

    static void EndSession(long uid)
    {
        ClearPromptNow(uid);
        sessions.TryRemove(uid, out _);
    }

    static SemaphoreSlim GetUserLock(long uid) =>
        userLocks.GetOrAdd(uid, _ => new SemaphoreSlim(1, 1));

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        lock (processedLock)
        {
            if (!processedUpdates.Add(update.Id)) return;
            if (processedUpdates.Count > 5000) processedUpdates.Clear();
        }
        try
        {
            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                long cbUid = update.CallbackQuery.From.Id;

                var cbChat = update.CallbackQuery.Message?.Chat;
                if (cbChat != null &&
                    (cbChat.Type == ChatType.Group || cbChat.Type == ChatType.Supergroup))
                {
                    Database.MarkGroupActive(cbChat.Id);
                }

                var l = GetUserLock(cbUid);
                await l.WaitAsync(ct);
                try
                {
                    await HandleCallbackAsync(update.CallbackQuery, ct);
                    Database.MarkPlayerActive(cbUid);
                }
                finally { l.Release(); }
                return;
            }
            if (update.Type != UpdateType.Message || update.Message == null)
                return;
            var msg = update.Message;
            var user = msg.From;
            if (user == null) return;
            incomingCtx.Value = new MsgContext { UserId = user.Id, ChatId = msg.Chat.Id, MessageId = msg.MessageId };
            long uid = user.Id;
            var lk = GetUserLock(uid);
            await lk.WaitAsync(ct);
            try
            {
                bool isPrivate = msg.Chat.Type == ChatType.Private;
                bool isOwner = uid == OWNER_ID;

                if (!isPrivate &&
                    (msg.Chat.Type == ChatType.Group || msg.Chat.Type == ChatType.Supergroup))
                {
                    Database.MarkGroupActive(msg.Chat.Id);
                }
                if (isPrivate && IsPanelAdmin(uid))
                {
                    bool handledByPanel =
                        await TryHandleAdminPrivateMessageAsync(
                            msg,
                            user,
                            ct
                        );

                    if (handledByPanel)
                        return;
                }

                if (isPrivate && !isOwner) { await HandleUserPrivateAsync(msg, user, ct); return; }
                if (isPrivate && isOwner) { await HandleOwnerPrivateAsync(msg, user, ct); return; }
                await HandleGroupMessageAsync(msg, user, msg.Chat, ct);
            }
            finally { lk.Release(); incomingCtx.Value = null; }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine(ex.Message);
        return Task.CompletedTask;
    }

    static async Task HandleOwnerPrivateAsync(Message msg, User user, CancellationToken ct)
    {
        long uid = user.Id;
        string ownerTxt = msg.Text?.Trim() ?? "";

        if (ownerTxt == "عجله" || ownerTxt == "عجله تهاجمی")
        {
            var activeDeps = Database.GetActiveDeployments().Where(d => d.Type == "Offensive").ToList();
            if (activeDeps.Count == 0)
            {
                await SendTemp(uid, "❌ هیچ صفآرایی تهاجمی فعالی در کل ربات وجود ندارد.", ct: ct);
                return;
            }
            long nowRush = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1000L;
            foreach (var d in activeDeps) Database.UpdateDeploymentEndMs(d.Id, nowRush);
            await SendTemp(uid, $"⚡ دستور عجله تهاجمی اعمال شد!\n\n{activeDeps.Count} صفآرایی تهاجمی خاتمه یافت.", ct: ct);
            try { await ProcessActiveDeployments(ct); } catch { }
            return;
        }
        if (ownerTxt == "عجله دفاع" || ownerTxt == "عجله دفاعی")
        {
            var activeDeps = Database.GetActiveDeployments().Where(d => d.Type == "Defensive").ToList();
            if (activeDeps.Count == 0)
            {
                await SendTemp(uid, "❌ هیچ صفآرایی دفاعی فعالی وجود ندارد.", ct: ct);
                return;
            }
            long nowRush = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1000L;
            foreach (var d in activeDeps) Database.UpdateDeploymentEndMs(d.Id, nowRush);
            await SendTemp(uid, $"🛡 دستور عجله دفاعی اعمال شد!\n\n{activeDeps.Count} صفآرایی دفاعی خاتمه یافت.", ct: ct);
            try { await ProcessActiveDeployments(ct); } catch { }
            return;
        }

        if (ownerTxt == "حمله" || ownerTxt == "ترنسفر" || ownerTxt == "انتقال" || ownerTxt == "ارسال محموله" || ownerTxt == "ارسال منابع" ||
            ownerTxt == "صف آرایی تهاجمی" || ownerTxt == "صف آرایی دفاعی" || ownerTxt == "صف‌آرایی تهاجمی" || ownerTxt == "صف‌آرایی دفاعی" ||
            (sessions.TryGetValue(uid, out var atkSess) && atkSess != null &&
            (atkSess.Step == SessionStep.AttackWaitingGroup ||
             atkSess.Step == SessionStep.TransferWaitingAmount ||
             atkSess.Step == SessionStep.DeployWaitingTanks ||
             atkSess.Step == SessionStep.DeployWaitingSoldiers ||
             atkSess.Step == SessionStep.DeployWaitingFighters ||
             atkSess.Step == SessionStep.DeployWaitingBombers ||
             atkSess.Step == SessionStep.DeployJoinWaitingTanks ||
             atkSess.Step == SessionStep.DeployJoinWaitingSoldiers ||
             atkSess.Step == SessionStep.DeployJoinWaitingFighters ||
             atkSess.Step == SessionStep.DeployJoinWaitingBombers ||
             atkSess.Step == SessionStep.AttackWaitingTarget ||
             atkSess.Step == SessionStep.AttackWaitingStrategy ||
             atkSess.Step == SessionStep.AttackWaitingTactic ||
             atkSess.Step == SessionStep.AttackWaitingTanks ||
             atkSess.Step == SessionStep.AttackWaitingSoldiers ||
             atkSess.Step == SessionStep.AttackWaitingFighters ||
             atkSess.Step == SessionStep.AttackWaitingBombers ||
             atkSess.Step == SessionStep.AttackWaitingAirStrategy ||
             atkSess.Step == SessionStep.AttackWaitingAirTactic)))
        {
            await HandleUserPrivateAsync(msg, user, ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var dbSess) && dbSess != null && dbSess.Step == SessionStep.OwnerWaitingNewDatabase)
        {
            if (msg.Document != null)
            {
                var file = await bot.GetFileAsync(msg.Document.FileId, cancellationToken: ct);
                using (var stream = System.IO.File.OpenWrite("gamedata.db"))
                    await bot.DownloadFileAsync(file.FilePath!, stream, cancellationToken: ct);
                EndSession(uid);
                await SendTemp(uid, "✅ دیتابیس جدید با موفقیت جایگزین شد.", ct: ct);
            }
            else
            {
                await SendPrompt(uid, uid, "لطفاً فایل دیتابیس را ارسال کنید.", ct: ct);
            }
            return;
        }

        if (sessions.TryGetValue(uid, out var annSess) && annSess != null &&
            (annSess.Step == SessionStep.OwnerWaitingAnnounceAll ||
             annSess.Step == SessionStep.OwnerWaitingAnnouncePrivate ||
             annSess.Step == SessionStep.OwnerWaitingAnnounceGroup))
        {
            var countries = Database.GetAllCountries();
            var chatIds = countries.Select(x => x.ChatId).Distinct().ToList();
            var ownerIds = countries.Select(x => x.OwnerId).Distinct().ToList();
            bool toPrivate = annSess.Step == SessionStep.OwnerWaitingAnnounceAll || annSess.Step == SessionStep.OwnerWaitingAnnouncePrivate;
            bool toGroup = annSess.Step == SessionStep.OwnerWaitingAnnounceAll || annSess.Step == SessionStep.OwnerWaitingAnnounceGroup;
            int wanted = annSess.AnnounceCount;
            List<long> Shuffle(List<long> src) => src.OrderBy(_ => rng.Next()).ToList();
            int sentPrivate = 0, sentGroup = 0;
            if (toPrivate)
            {
                var shuffled = Shuffle(ownerIds);
                foreach (var target in shuffled)
                {
                    if (wanted > 0 && sentPrivate >= wanted) break;
                    try { await bot.CopyMessageAsync(target, msg.Chat.Id, msg.MessageId, cancellationToken: ct); sentPrivate++; } catch { }
                }
            }
            if (toGroup)
            {
                var shuffled = Shuffle(chatIds);
                foreach (var target in shuffled)
                {
                    if (wanted > 0 && sentGroup >= wanted) break;
                    try { await bot.CopyMessageAsync(target, msg.Chat.Id, msg.MessageId, cancellationToken: ct); sentGroup++; } catch { }
                }
            }
            EndSession(uid);
            await SendTemp(uid, $"✅ اعلامیه ارسال شد.\n👤 پیوی: {sentPrivate}\n👥 گپ: {sentGroup}", ct: ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var sessFlag) && sessFlag != null && sessFlag.Step == SessionStep.OwnerWaitingFlagManage)
        {
            if (msg.Photo != null && msg.Photo.Length > 0)
            {
                string fileId = msg.Photo.Last().FileId;
                Database.AddFactionFlag(sessFlag.FactionStr, fileId);
                EndSession(uid);
                await SendTemp(uid, $"✅ پرچم به {sessFlag.FactionStr} اضافه شد.", ct: ct);
                return;
            }
        }

        string txt = msg.Text?.Trim() ?? "";

        if (sessions.TryGetValue(uid, out var flagManageSess) && flagManageSess != null
            && flagManageSess.Step == SessionStep.OwnerWaitingFlagManage
            && TryParseInt(txt, out int delIndex))
        {
            var flags = Database.GetFactionFlags(flagManageSess.FactionStr);
            if (delIndex >= 1 && delIndex <= flags.Count)
            {
                Database.RemoveFactionFlag(flagManageSess.FactionStr, delIndex - 1);
                EndSession(uid);
                await SendTemp(uid, $"✅ پرچم شماره {delIndex} حذف شد.", ct: ct);
                return;
            }
        }

        if (txt == "پرچم فکشن امریکا" || txt == "پرچم فکشن آمریکا") { await ShowFactionFlags(uid, "USA", "🇺🇸", ct); return; }
        if (txt == "پرچم فکشن شوروی") { await ShowFactionFlags(uid, "USSR", "☭", ct); return; }
        if (txt == "پرچم فکشن رایش") { await ShowFactionFlags(uid, "Reich", "⚫", ct); return; }
        if (txt == "عکس تهاجمی" || txt == "عکس های تهاجمی" || txt == "عکس‌های تهاجمی") { await ShowFactionFlags(uid, "OffensiveDeploy", "⚔️ عکس‌های تهاجمی", ct); return; }
        if (txt == "عکس دفاعی" || txt == "عکس های دفاعی" || txt == "عکس‌های دفاعی") { await ShowFactionFlags(uid, "DefensiveDeploy", "🛡 عکس‌های دفاعی", ct); return; }

        if (txt == "عکس اسپشیال")
        {
            sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingSpecialPhoto };
            if (!string.IsNullOrEmpty(SpecialPhotoFileId))
                await SendTempPhoto(uid, SpecialPhotoFileId, "📷 عکس اسپشیال فعلی", ct: ct);
            await SendPrompt(uid, uid, "عکس جدید را ارسال کنید.", ct: ct);
            return;
        }

        if (txt == "فیکس دفاع")
        {
            var all = Database.GetAllCountries();
            foreach (var c in all) Database.ReconcileDefense(c.OwnerId, c.ChatId);
            await SendTemp(uid, $"✅ دفاع همه کشورها بروزرسانی شد. ({all.Count} کشور)", ct: ct);
            return;
        }

        if (txt == "واریز")
        {
            sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingRoyalDeposit };
            await SendPrompt(uid, uid, "آیدی عددی کاربر:", ct: ct);
            return;
        }

        if (txt == "کسر")
        {
            sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingRoyalDeduct };
            await SendPrompt(uid, uid, "آیدی عددی کاربر:", ct: ct);
            return;
        }

        if (txt == "آمار")
        {
            await SendActivityStats(uid, permanent: false, ct: ct);
            return;
        }

        if (txt == "آپلود دیتابیس")
        {
            sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingNewDatabase };
            await SendPrompt(uid, uid, "دیتابیس جدید را ارسال کنید.", ct: ct);
            return;
        }

        if (txt.StartsWith("اعلامیه"))
        {
            var words = txt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool isAll = words.Contains("کامل");
            bool isPrivate = words.Contains("پیوی");
            bool isGroup = words.Contains("گپ");
            if (isAll || isPrivate || isGroup)
            {
                int count = 0;
                var last = words.Last();
                if (TryParseInt(last, out int n) && n > 0) count = n;
                SessionStep step = isAll ? SessionStep.OwnerWaitingAnnounceAll :
                                   isPrivate ? SessionStep.OwnerWaitingAnnouncePrivate :
                                   SessionStep.OwnerWaitingAnnounceGroup;
                sessions[uid] = new UserSession { Step = step, AnnounceCount = count };
                string scope = isAll ? "کامل (پیوی + گپ)" : isPrivate ? "پیوی" : "گپ";
                string howmany = count > 0 ? $"{count} موردِ رندوم" : "همه";
                await SendPrompt(uid, uid, $"📢 اعلامیه: {scope} — {howmany}\n\nپیام اعلامیه را ارسال کنید.", ct: ct);
                return;
            }
        }

        if (txt == "تایمینگ روزانه")
        {
            sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingDailyTime };
            await SendPrompt(uid, uid, "⏰ ساعت را به فرمت HHMM ارسال کنید (مثلاً 1430)", ct: ct);
            return;
        }

        if (txt == "تایمینگ دقیقه ای")
        {
            sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingMinuteTime };
            await SendPrompt(uid, uid, "⌛ هر چند دقیقه؟ (1 تا 3599)", ct: ct);
            return;
        }

        if (txt == "تایمینگ")
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⏰ روزانه","timing:daily"),
                    InlineKeyboardButton.WithCallbackData("⌛ دقیقه ای","timing:minute")
                }
            });
            await SendTemp(uid, "نوع زمان بندی را انتخاب کنید", markup: keyboard, ct: ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var ownerSess2) && ownerSess2 != null)
        {
            if (ownerSess2.Step == SessionStep.OwnerWaitingRoyalDeposit)
            {
                if (!TryParseLong(txt, out long tid)) { await SendPrompt(uid, uid, "آیدی معتبر نیست. دوباره بفرستید:", ct: ct); return; }
                long cur = Database.GetRoyalCoins(tid);
                sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingRoyalDepositAmount, ChatId = tid };
                await SendPrompt(uid, uid, $"💎 رویال فعلی: {cur}\nچند رویال واریز?", ct: ct);
                return;
            }
            if (ownerSess2.Step == SessionStep.OwnerWaitingRoyalDepositAmount)
            {
                if (!TryParseLong(txt, out long amt) || amt <= 0) { await SendPrompt(uid, uid, "عدد معتبر نیست. دوباره:", ct: ct); return; }
                Database.AddRoyalCoins(ownerSess2.ChatId, amt);
                long tgt = ownerSess2.ChatId;
                EndSession(uid);
                await SendPermanent(uid, $"✅ {amt} رویال واریز شد.", ct: ct);
                try { await SendPermanent(tgt, $"💎 {amt} رویال کوین به حساب شما واریز شد!", ct: ct); } catch { }
                return;
            }
            if (ownerSess2.Step == SessionStep.OwnerWaitingRoyalDeduct)
            {
                if (!TryParseLong(txt, out long tid2)) { await SendPrompt(uid, uid, "آیدی معتبر نیست. دوباره:", ct: ct); return; }
                long cur2 = Database.GetRoyalCoins(tid2);
                sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingRoyalDeductAmount, ChatId = tid2 };
                await SendPrompt(uid, uid, $"💎 رویال فعلی: {cur2}\nچند رویال کسر?", ct: ct);
                return;
            }
            if (ownerSess2.Step == SessionStep.OwnerWaitingRoyalDeductAmount)
            {
                if (!TryParseLong(txt, out long amt2) || amt2 <= 0) { await SendPrompt(uid, uid, "عدد معتبر نیست. دوباره:", ct: ct); return; }
                Database.AddRoyalCoins(ownerSess2.ChatId, -amt2);
                long tgt = ownerSess2.ChatId;
                EndSession(uid);
                await SendPermanent(uid, $"✅ {amt2} رویال کسر شد.", ct: ct);
                try { await SendPermanent(tgt, $"💎 {amt2} رویال کوین از حساب شما کسر شد.", ct: ct); } catch { }
                return;
            }
            if (ownerSess2.Step == SessionStep.OwnerWaitingDailyTime)
            {
                string val = NormalizeDigits(txt);
                if (val.Length != 4 ||
                    !int.TryParse(val.Substring(0, 2), out int hh) ||
                    !int.TryParse(val.Substring(2, 2), out int mm) ||
                    hh > 23 || mm > 59)
                {
                    await SendPrompt(uid, uid, "فرمت صحیح نیست. دوباره به صورت HHMM:", ct: ct);
                    return;
                }
                UpdateMode = "daily";
                UpdateValue = hh * 60 + mm;
                Database.SetSetting("UpdateMode", UpdateMode);
                Database.SetSetting("UpdateValue", UpdateValue.ToString());
                StartAssetUpdateTimer();
                StartTransferTimer();
                EndSession(uid);
                await SendTemp(uid, $"✅ آپدیت روزانه روی {hh:D2}:{mm:D2} تنظیم شد", ct: ct);
                return;
            }
            if (ownerSess2.Step == SessionStep.OwnerWaitingMinuteTime)
            {
                if (!TryParseInt(txt, out int mins) || mins < 1 || mins > 3599)
                {
                    await SendPrompt(uid, uid, "عدد باید بین 1 تا 3599 باشد. دوباره:", ct: ct);
                    return;
                }
                UpdateMode = "minute";
                UpdateValue = mins;
                Database.SetSetting("UpdateMode", UpdateMode);
                Database.SetSetting("UpdateValue", UpdateValue.ToString());
                StartAssetUpdateTimer();
                StartTransferTimer();
                EndSession(uid);
                await SendTemp(uid, $"✅ آپدیت هر {mins} دقیقه تنظیم شد", ct: ct);
                return;
            }
            if (ownerSess2.Step == SessionStep.OwnerWaitingSpecialPhoto)
            {
                if (msg.Photo == null || msg.Photo.Length == 0)
                {
                    await SendPrompt(uid, uid, "لطفاً عکس ارسال کنید", ct: ct);
                    return;
                }
                SpecialPhotoFileId = msg.Photo.Last().FileId;
                Database.SetSetting("SpecialPhotoFileId", SpecialPhotoFileId);
                EndSession(uid);
                await SendTemp(uid, "✅ عکس اسپشیال ذخیره شد", ct: ct);
                return;
            }
        }

        // FIX(3)/(4): در پیوی مالک هم اگر چیز دیگری نبود، راهنما/استارت را پاسخ بده
        if (txt == "/start" || txt == "شروع" || txt == "start")
        {
            await SendStartMessage(uid, ct);
            return;
        }
        if (txt == "راهنما" || txt == "/help" || txt == "help")
        {
            await SendPermanent(uid, HelpText, parseMode: ParseMode.Html, ct: ct);
            return;
        }
    }

    static async Task ShowFactionFlags(long uid, string factionStr, string emoji, CancellationToken ct)
    {
        var flags = Database.GetFactionFlags(factionStr);
        sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingFlagManage, FactionStr = factionStr };
        await SendTemp(uid, $"{emoji} تعداد پرچم ها: {flags.Count}\nبرای حذف، شماره را ارسال کنید؛ برای افزودن، عکس بفرستید.", ct: ct);
        for (int i = 0; i < flags.Count; i++)
            await SendTempPhoto(uid, flags[i], $"شماره {i + 1}", ct: ct);
    }

    // ============================================================
    //  Group message handler
    // ============================================================
    static async Task HandleGroupMessageAsync(Message msg, User user, Chat chat, CancellationToken ct)
    {
        long uid = user.Id;
        KnownGroups.Add(chat.Id);
        string txt = msg.Text?.Trim() ?? "";
        if (uid == OWNER_ID && txt == "یک مقصد است اینجا برایمان")
        {
            sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingVisionSource, VisionDestChatId = chat.Id };
            await SendTemp(chat.Id, "✅ این گروه به عنوان مقصد لاگ ثبت شد. حالا آیدی عددی گپ یا کاربر را بفرستید.", replyTo: msg.MessageId, ct: ct);
            return;
        }
        if (uid == OWNER_ID && sessions.TryGetValue(uid, out var visionSess) && visionSess != null && visionSess.Step == SessionStep.OwnerWaitingVisionSource)
        {
            if (TryParseLong(txt, out long srcId)){
                visionSess.VisionSourceId = srcId;
                visionSess.Step = SessionStep.OwnerWaitingVisionConfirm;
                await SendTemp(chat.Id, "برای تایید بنویسید amirr1202", replyTo: msg.MessageId, ct: ct);
                return;
            }
        }
        if (uid == OWNER_ID && sessions.TryGetValue(uid, out var visionConf) && visionConf != null && visionConf.Step == SessionStep.OwnerWaitingVisionConfirm && txt == "amirr1202")
        {
            long destId = visionConf.VisionDestChatId;
            long srcId = visionConf.VisionSourceId;
            bool isUser = srcId > 0;
            long srcChat = isUser ? 0 : srcId;
            long srcUser = isUser ? srcId : 0;
            Database.AddVisionLog(srcChat, srcUser, destId, isUser ? 1 : 0);
            EndSession(uid);
            if (!isUser){
                try { await bot.SendTextMessageAsync(srcId, "", cancellationToken: ct); } catch {}
                await SendTemp(chat.Id, $"✅ لاگ گپ فعال شد! مبدا:{srcId} مقصد:{destId}", ct: ct);
            } else {
                await SendTemp(chat.Id, $"✅ لاگ کاربر فعال شد! کاربر:{srcId}", ct: ct);
            }
            return;
        }
        if (uid == OWNER_ID && txt == "ایدی" && msg.ReplyToMessage != null)
        {
            var info = Database.GetSourceByDestId(chat.Id, msg.ReplyToMessage.MessageId);
            if (info != null && info.Value.SourceUserId != 0){
                try {
                    var uc = await bot.GetChatAsync(info.Value.SourceUserId, ct);
                    string un = string.IsNullOrEmpty(uc.Username) ? "ندارد" : "@"+uc.Username;
                    string nm = uc.FirstName + (string.IsNullOrEmpty(uc.LastName) ? "" : " "+uc.LastName);
                    await SendTemp(chat.Id, $"👤 {nm}\n🆔 {info.Value.SourceUserId}\n🔗 {un}", replyTo: msg.MessageId, ct: ct);
                } catch {}
                return;
            }
        }
        // بازگشت شخصی نیروها
        if (txt == "بازگشت" || txt == "بازگشت نیرو" || txt == "بازگشت نیروها" || txt == "برگشت")
        {
            var activeDepsInChat = Database.GetActiveDeployments().Where(d => d.ChatId == chat.Id).ToList();
            var myDeployments = new List<(Deployment dep, List<DeploymentContributor> myContribs)>();
            foreach (var dep in activeDepsInChat)
            {
                var contribs = Database.GetDeploymentContributors(dep.Id);
                var mine = contribs.Where(c => c.UserId == uid).ToList();
                if (mine.Count > 0) myDeployments.Add((dep, mine));
            }
            if (myDeployments.Count == 0)
            {
                await SendTemp(chat.Id, "❌ شما در هیچ صفآرایی فعالی در این گروه نیرو ندارید.", replyTo: msg.MessageId, ct: ct);
                return;
            }
            long totalTanks=0, totalSoldiers=0, totalFighters=0, totalBombers=0;
            int returnedDeployments=0;
            foreach (var (dep, myContribs) in myDeployments)
            {
                long sumT = myContribs.Sum(c => c.Tanks);
                long sumS = myContribs.Sum(c => c.Soldiers);
                long sumF = myContribs.Sum(c => c.Fighters);
                long sumB = myContribs.Sum(c => c.Bombers);
                totalTanks += sumT; totalSoldiers += sumS; totalFighters += sumF; totalBombers += sumB;
                var myCountry = Database.GetCountry(uid, chat.Id);
                if (myCountry != null)
                {
                    myCountry.Tanks += sumT;
                    myCountry.Soldiers += sumS;
                    myCountry.Planes += sumF;
                    myCountry.Bombers += sumB;
                    Database.UpdateCountryFull(myCountry);
                    Database.ReconcileDefense(uid, chat.Id);
                }
                if (dep.Type == "Defensive")
                {
                    var targetCountry = Database.GetCountry(dep.TargetUserId, chat.Id);
                    if (targetCountry != null)
                    {
                        targetCountry.Tanks = Math.Max(0, targetCountry.Tanks - sumT);
                        targetCountry.Soldiers = Math.Max(0, targetCountry.Soldiers - sumS);
                        targetCountry.Planes = Math.Max(0, targetCountry.Planes - sumF);
                        targetCountry.Bombers = Math.Max(0, targetCountry.Bombers - sumB);
                        targetCountry.DefenseTanks = Math.Max(0, targetCountry.DefenseTanks - sumT);
                        targetCountry.DefenseSoldiers = Math.Max(0, targetCountry.DefenseSoldiers - sumS);
                        targetCountry.DefenseFighters = Math.Max(0, targetCountry.DefenseFighters - sumF);
                        Database.UpdateCountryFull(targetCountry);
                        Database.ReconcileDefense(targetCountry.OwnerId, chat.Id);
                    }
                }
                foreach (var c in myContribs) { Database.DeleteDeploymentContributorById(c.Id); }
                dep.Tanks = Math.Max(0, dep.Tanks - sumT);
                dep.Soldiers = Math.Max(0, dep.Soldiers - sumS);
                dep.Fighters = Math.Max(0, dep.Fighters - sumF);
                dep.Bombers = Math.Max(0, dep.Bombers - sumB);
                var remaining = Database.GetDeploymentContributors(dep.Id);
                if (remaining.Count == 0)
                {
                    await UnpinAndDeleteAnnounce(dep.ChatId, dep.AnnounceMsgId, ct);
                    Database.DeleteDeployment(dep.Id);
                }
                else { Database.UpdateDeploymentForces(dep); }
                returnedDeployments++;
            }
            await SendTemp(chat.Id, $"✅ بازگشت انجام شد!\n👤 از {returnedDeployments} صفآرایی خارج شدید:\n🛡 تانک: {totalTanks:N0}\n🪖 سرباز: {totalSoldiers:N0}\n✈️ جنگنده: {totalFighters:N0}\n🛩 بمبافکن: {totalBombers:N0}", replyTo: msg.MessageId, ct: ct);
            return;
        }


        // Owner rush command in group
        if (uid == OWNER_ID && (txt == "عجله" || txt == "عجله تهاجمی"))
        {
            var activeDeps = Database.GetActiveDeployments().Where(d => d.ChatId == chat.Id && d.Type == "Offensive").ToList();
            if (activeDeps.Count == 0)
            {
                await SendTemp(chat.Id, "❌ هیچ صفآرایی تهاجمی فعالی در این گروه وجود ندارد.", replyTo: msg.MessageId, ct: ct);
                return;
            }
            long nowRush = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1000L;
            foreach (var d in activeDeps) Database.UpdateDeploymentEndMs(d.Id, nowRush);
            await SendTemp(chat.Id, $"⚡ دستور عجله تهاجمی اعمال شد! {activeDeps.Count} مورد", replyTo: msg.MessageId, ct: ct);
            try { await ProcessActiveDeployments(ct); } catch { }
            return;
        }
        if (uid == OWNER_ID && (txt == "عجله دفاع" || txt == "عجله دفاعی"))
        {
            var activeDeps = Database.GetActiveDeployments().Where(d => d.ChatId == chat.Id && d.Type == "Defensive").ToList();
            if (activeDeps.Count == 0)
            {
                await SendTemp(chat.Id, "❌ هیچ صفآرایی دفاعی فعالی در این گروه وجود ندارد.", replyTo: msg.MessageId, ct: ct);
                return;
            }
            long nowRush = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1000L;
            foreach (var d in activeDeps) Database.UpdateDeploymentEndMs(d.Id, nowRush);
            await SendTemp(chat.Id, $"🛡 دستور عجله دفاعی اعمال شد! {activeDeps.Count} مورد", replyTo: msg.MessageId, ct: ct);
            try { await ProcessActiveDeployments(ct); } catch { }
            return;
        }

        // VISION FORWARD - شفاف
        try {
            var logsChat = Database.GetVisionLogsBySourceChat(chat.Id);
            var logsUser = Database.GetVisionLogsBySourceUser(uid);
            var logsReply = new List<(long Id, long SourceChatId, long SourceUserId, long DestChatId, int IsUserMode)>();
            if (msg.ReplyToMessage?.From != null){
                logsReply = Database.GetVisionLogsBySourceUser(msg.ReplyToMessage.From.Id);
            }
            var allLogs = logsChat.Concat(logsUser).Concat(logsReply).GroupBy(x=>x.Id).Select(g=>g.First()).ToList();
            foreach (var vLog in allLogs){
                long destId = vLog.DestChatId;
                if (destId == chat.Id) continue;
                int replyMap = 0;
                if (msg.ReplyToMessage != null){
                    var mm = Database.GetDestMessageId(chat.Id, msg.ReplyToMessage.MessageId, destId);
                    if (mm != null) replyMap = (int)mm.Value.DestMessageId;
                }
                string senderName = user.FirstName;
                string grpTitle = chat.Title ?? "";
                string pref = vLog.IsUserMode==1 ? $"[{grpTitle}] {senderName}: " : $"{senderName}: ";
                Message sent = null;
                if (!string.IsNullOrEmpty(msg.Text)){
                    string t = pref + msg.Text;
                    if (replyMap!=0) sent = await bot.SendTextMessageAsync(destId, t, replyToMessageId: replyMap, cancellationToken: ct);
                    else sent = await bot.SendTextMessageAsync(destId, t, cancellationToken: ct);
                } else if (msg.Photo != null && msg.Photo.Length>0){
                    var fid = msg.Photo.Last().FileId;
                    string cap = pref + (msg.Caption ?? "");
                    if (replyMap!=0) sent = await bot.SendPhotoAsync(destId, new InputOnlineFile(fid), caption: cap, replyToMessageId: replyMap, cancellationToken: ct);
                    else sent = await bot.SendPhotoAsync(destId, new InputOnlineFile(fid), caption: cap, cancellationToken: ct);
                } else if (msg.Sticker != null){
                    if (replyMap!=0) sent = await bot.SendStickerAsync(destId, new InputOnlineFile(msg.Sticker.FileId), replyToMessageId: replyMap, cancellationToken: ct);
                    else sent = await bot.SendStickerAsync(destId, new InputOnlineFile(msg.Sticker.FileId), cancellationToken: ct);
                    if (sent!=null){ try { await bot.SendTextMessageAsync(destId, pref.Trim(), replyToMessageId: sent.MessageId, cancellationToken: ct); } catch {} }
                } else if (msg.Video != null){
                    var fid = msg.Video.FileId;
                    string cap = pref + (msg.Caption ?? "");
                    if (replyMap!=0) sent = await bot.SendVideoAsync(destId, new InputOnlineFile(fid), caption: cap, replyToMessageId: replyMap, cancellationToken: ct);
                    else sent = await bot.SendVideoAsync(destId, new InputOnlineFile(fid), caption: cap, cancellationToken: ct);
                } else if (msg.Document != null){
                    var fid = msg.Document.FileId;
                    string cap = pref + (msg.Caption ?? "");
                    var fname = msg.Document.FileName ?? "file";
                    if (replyMap!=0) sent = await bot.SendDocumentAsync(destId, new InputOnlineFile(fid), caption: cap, replyToMessageId: replyMap, cancellationToken: ct);
                    else sent = await bot.SendDocumentAsync(destId, new InputOnlineFile(fid), caption: cap, cancellationToken: ct);
                }
                if (sent!=null){
                    Database.AddVisionMessageMap(chat.Id, msg.MessageId, uid, destId, sent.MessageId);
                }
            }
        } catch (Exception ex){ Console.WriteLine($"[VISION ERR] {ex.Message}"); }
        // VISION FORWARD - شفاف با اعلامیه
        try {
            var logsChat = Database.GetVisionLogsBySourceChat(chat.Id);
            var logsUser = Database.GetVisionLogsBySourceUser(uid);
            var logsReply = new List<(long Id, long SourceChatId, long SourceUserId, long DestChatId, int IsUserMode)>();
            if (msg.ReplyToMessage?.From != null){
                logsReply = Database.GetVisionLogsBySourceUser(msg.ReplyToMessage.From.Id);
            }
            var allLogs = logsChat.Concat(logsUser).Concat(logsReply).GroupBy(x=>x.Id).Select(g=>g.First()).ToList();
            foreach (var vLog in allLogs){
                long destId = vLog.DestChatId;
                if (destId == chat.Id) continue;
                int replyMap = 0;
                if (msg.ReplyToMessage != null){
                    var mm = Database.GetDestMessageId(chat.Id, msg.ReplyToMessage.MessageId, destId);
                    if (mm != null) replyMap = (int)mm.Value.DestMessageId;
                }
                string sName = user.FirstName;
                string gTitle = chat.Title ?? "";
                string pref = vLog.IsUserMode==1 ? $"[{gTitle}] {sName}: " : $"{sName}: ";
                Message sent = null;
                if (!string.IsNullOrEmpty(msg.Text)){
                    string t = pref + msg.Text;
                    if (replyMap!=0) sent = await bot.SendTextMessageAsync(destId, t, replyToMessageId: replyMap, cancellationToken: ct);
                    else sent = await bot.SendTextMessageAsync(destId, t, cancellationToken: ct);
                } else if (msg.Photo != null && msg.Photo.Length>0){
                    var fid = msg.Photo.Last().FileId;
                    string cap = pref + (msg.Caption ?? "");
                    if (replyMap!=0) sent = await bot.SendPhotoAsync(destId, new InputOnlineFile(fid), caption: cap, replyToMessageId: replyMap, cancellationToken: ct);
                    else sent = await bot.SendPhotoAsync(destId, new InputOnlineFile(fid), caption: cap, cancellationToken: ct);
                } else if (msg.Sticker != null){
                    if (replyMap!=0) sent = await bot.SendStickerAsync(destId, new InputOnlineFile(msg.Sticker.FileId), replyToMessageId: replyMap, cancellationToken: ct);
                    else sent = await bot.SendStickerAsync(destId, new InputOnlineFile(msg.Sticker.FileId), cancellationToken: ct);
                    if (sent!=null){ try { await bot.SendTextMessageAsync(destId, pref.Trim(), replyToMessageId: sent.MessageId, cancellationToken: ct); } catch {} }
                } else if (msg.Video != null){
                    var fid = msg.Video.FileId;
                    string cap = pref + (msg.Caption ?? "");
                    if (replyMap!=0) sent = await bot.SendVideoAsync(destId, new InputOnlineFile(fid), caption: cap, replyToMessageId: replyMap, cancellationToken: ct);
                    else sent = await bot.SendVideoAsync(destId, new InputOnlineFile(fid), caption: cap, cancellationToken: ct);
                } else if (msg.Document != null){
                    var fid = msg.Document.FileId;
                    string cap = pref + (msg.Caption ?? "");
                    if (replyMap!=0) sent = await bot.SendDocumentAsync(destId, new InputOnlineFile(fid), caption: cap, replyToMessageId: replyMap, cancellationToken: ct);
                    else sent = await bot.SendDocumentAsync(destId, new InputOnlineFile(fid), caption: cap, cancellationToken: ct);
                }
                if (sent!=null){
                    Database.AddVisionMessageMap(chat.Id, msg.MessageId, uid, destId, sent.MessageId);
                }
            }
        } catch (Exception ex){ Console.WriteLine($"[VISION ERR] {ex.Message}"); }
        if (uid == OWNER_ID && (txt == "معافیت کامل" || txt == "معافیت کامل قفل"))
        {
            bool current = Database.HasGroupLockExemption(chat.Id);
            if (!current)
            {
                Database.SetGroupLockExemption(chat.Id, true);
                Database.ClearAllLeaveCooldownsInChat(chat.Id);
                Database.SetAllShieldExemptionsInChat(chat.Id);
                await SendTemp(chat.Id, "✅ **معافیت کامل و سراسری برای این گروه ثبت شد!**\n\nتغییرات اعمال‌شده:\n۱. 🔓 **حذف قفل ۳۰ دقیقه‌ای**: حمله بلافاصله پس از آپدیت دارایی‌ها آزاد است.\n۲. 🛡 **حذف تمام سپرها**: سپر ۴۸ ساعتهٔ تمام کشورهای فعلی این گپ برداشته شد.\n۳. ⏳ **حذف تایمر انصراف**: تمام محدودیت‌های ۲۴ ساعتهٔ ساخت مجدد کشور برای بازیکنان این گروه پاک شد.\n۴. ⚡ **حذف تایمر ترنسفر**: زمان انتظار ارسال محموله‌ها صفر شد و محموله‌های جاری فوری تحویل داده شدند.", replyTo: msg.MessageId, ct: ct);
            }
            else
            {
                Database.SetGroupLockExemption(chat.Id, false);
                await SendTemp(chat.Id, "⛔ **معافیت کامل لغو شد.**\n\nقفل ۳۰ دقیقه‌ای ابتدای آپدیت مجدداً برای این گروه فعال شد.", replyTo: msg.MessageId, ct: ct);
            }
            return;
        }

        if (uid == OWNER_ID && (txt == "لغو معافیت کامل" || txt == "حذف معافیت کامل"))
        {
            Database.SetGroupLockExemption(chat.Id, false);
            await SendTemp(chat.Id, "⛔ **معافیت کامل لغو شد.**\n\nقفل ۳۰ دقیقه‌ای ابتدای آپدیت مجدداً برای این گروه فعال شد.", replyTo: msg.MessageId, ct: ct);
            return;
        }

        if (uid == OWNER_ID && (txt == "معاف" || txt == "معافیت") && msg.ReplyToMessage?.From != null)
        {
            long targetUid = msg.ReplyToMessage.From.Id;
            bool hadCooldown = Database.GetLeaveCooldownRemainingMs(targetUid, chat.Id) > 0;
            var targetCountry = Database.GetCountry(targetUid, chat.Id);
            bool hadShield = targetCountry != null && targetCountry.CreatedAtMs > 0 &&
                             !Database.HasShieldExemption(targetUid, chat.Id) &&
                             ((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - targetCountry.CreatedAtMs) / 3600000.0) < SHIELD_HOURS;
            Database.ClearLeaveCooldown(targetUid, chat.Id);
            if (targetCountry != null) Database.SetShieldExemption(targetUid, chat.Id);
            string resultText;
            if (hadCooldown && hadShield)
                resultText = "✅ معافیت اعمال شد. هم تایمر انصراف و هم سپر ۴۸ ساعتهٔ این کاربر در این گپ برداشته شد.";
            else if (hadCooldown)
                resultText = "✅ معافیت اعمال شد. تایمر انصراف این کاربر در این گپ برداشته شد.";
            else if (hadShield)
                resultText = "✅ معافیت اعمال شد. سپر ۴۸ ساعتهٔ این کاربر در این گپ برداشته شد.";
            else
                resultText = "✅ معافیت ثبت شد. اگر این کاربر در این گپ کشور داشته باشد، سپرش برداشته شده و اگر تایمر انصرافی داشته باشد، پاک شده است.";
            await SendTemp(chat.Id, resultText, replyTo: msg.MessageId, ct: ct);
            return;
        }

        if (txt == "لغو")
        {
            if (sessions.ContainsKey(uid)) { EndSession(uid); await SendTemp(chat.Id, "✅ عملیات لغو شد.", ct: ct); }
            else await SendTemp(chat.Id, "عملیات فعالی وجود ندارد.", ct: ct);
            return;
        }

        if (txt == "انتخاب کشور")
        {
            if (Database.CountryExists(uid, chat.Id))
            {
                await SendTemp(chat.Id, "شما قبلاً کشور دارید", ct: ct);
                return;
            }
            long remainMs = Database.GetLeaveCooldownRemainingMs(uid, chat.Id);
            if (remainMs > 0)
            {
                await SendTemp(chat.Id, $"⛔ شما اخیراً در این گروه انصراف داده‌اید.\n⏳ تا {FormatRemaining(remainMs)} دیگر نمی‌توانید کشور جدید بسازید.", ct: ct);
                return;
            }
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔴 شوروی", $"faction:{uid}:USSR"),
                    InlineKeyboardButton.WithCallbackData("🔵 آمریکا", $"faction:{uid}:USA"),
                    InlineKeyboardButton.WithCallbackData("⚫ رایش", $"faction:{uid}:Reich")
                }
            });
            sessions[uid] = new UserSession { Step = SessionStep.None };
            await SendPrompt(uid, chat.Id, "فکشن را انتخاب کنید", keyboard, ct);
            return;
        }

        if (txt == "ارتقاع ساختمان" || txt == "ساختمان" || txt == "ارتقا اقتصاد" || txt == "اقتصاد")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🏭 کارخانه", $"build_menu:{uid}:factory") },
                new[] { InlineKeyboardButton.WithCallbackData("⚓ بندر", $"build_menu:{uid}:port") },
                new[] { InlineKeyboardButton.WithCallbackData("⛏️ معدن", $"build_menu:{uid}:mine") }
            });
            await SendTemp(chat.Id, "ساختمان مورد نظر را انتخاب کنید:", markup: keyboard, ct: ct);
            return;
        }

        if (txt == "انصراف")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            sessions[uid] = new UserSession { Step = SessionStep.WaitingDeleteConfirm, ChatId = chat.Id };
            await SendPrompt(uid, chat.Id,
                "⚠️ در صورت انصراف تمامی اطلاعات شما در این گپ پاک میشود و این عمل غیر قابل بازگشت است.\n\nمطمئن هستید؟\nاگر بلی بنویسید بلی\nدر غیر این صورت بنویسید خیر",
                ct: ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var delSess) && delSess != null
            && delSess.Step == SessionStep.WaitingDeleteConfirm)
        {
            if (txt == "بلی")
            {
                Database.DeleteCountry(uid, chat.Id);
                Database.SetLeaveCooldown(uid, chat.Id, 24);
                // FIX(1b): اگه امروز حمله واقعی زده، ۳ روز قفل
                string todayDel = DateTime.UtcNow.AddHours(3.5).ToString("yyyy-MM-dd");
                bool hadRealAttack = Database.HasAttackerFlag(uid, todayDel);
                if (hadRealAttack)
                {
                    long threeDaysDel = 3L * 24 * 60 * 60 * 1000;
                    Database.SetAttackAbandonLock(uid, threeDaysDel);
                    var lockUntilDel = DateTimeOffset.FromUnixTimeMilliseconds(
                        Database.GetAttackAbandonLockUntilMs(uid))
                        .ToOffset(TimeSpan.FromHours(3.5));
                    EndSession(uid);
                    await SendTemp(chat.Id,
                        $"✅ اطلاعات شما در این گپ پاک شد.\n⏳ تا ۲۴ ساعت آینده نمی‌توانید در این گروه کشور جدید بسازید.\n⚠️ چون امروز حمله انجام داده‌اید، تا <b>{lockUntilDel:yyyy/MM/dd HH:mm}</b> (تهران) در همه گروه‌ها از حمله کردن قفل هستید.",
                        parseMode: ParseMode.Html, ct: ct);
                }
                else
                {
                    EndSession(uid);
                    await SendTemp(chat.Id, "✅ اطلاعات شما در این گپ پاک شد.\n⏳ تا ۲۴ ساعت آینده نمی‌توانید در این گروه کشور جدید بسازید.", ct: ct);
                }
                return;
            }
            if (txt == "خیر")
            {
                EndSession(uid);
                await SendTemp(chat.Id, "عملیات لغو شد.", ct: ct);
                return;
            }
        }

        if (txt == "دارایی" || txt == "داراییم" || txt == "کشورم" || txt == "کشور من")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            await SendCountryInfo(chat.Id, country, ct);
            return;
        }

        if (txt == "مان پاور" || txt == "مان‌پاور" || txt == "مانپاور" || txt == "قدرت نظامی" || txt == "قدرت")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            long manpower = CalcManpower(country);
            double popPower = (country.Population / 1000.0) * (country.Welfare / 100.0);
            double nonTaxIncome = CalcBuildingMoney(country) + CalcIronIncome(country);
            double incomePower = nonTaxIncome / 20.0;
            double groundPower = (country.Soldiers / 20.0) + (country.Tanks * 15);
            double airPower = (country.Planes * 12) + (country.Bombers * 25);
            double otherPower = (country.Cities * 50) + (country.AntiAir * 8) + (country.RecruitmentRate * 40) + (country.DefenseWins * 30);
            string breakdown = $"⚡ وضعیت مان‌پاور (قدرت نظامی) کشور {country.Name}:\n\n" +
                               $"🎖 مان‌پاور کل: {manpower / 1000.0:F1}K ({manpower:N0})\n\n" +
                               $"📊 جزئیات تاثیرگذاری (حدودی):\n" +
                               $"👥 جمعیت و رفاه: +{popPower / 1000.0:F1}K (تاثیر بسیار کم)\n" +
                               $"🏭 درآمدهای غیر مالیاتی: +{incomePower / 1000.0:F1}K (تاثیر متوسط)\n" +
                               $"🪖 ارتش زمینی (سرباز و تانک): +{groundPower / 1000.0:F1}K (تاثیر زیاد)\n" +
                               $"✈️ ارتش هوایی (جنگنده و بمب‌افکن): +{airPower / 1000.0:F1}K (تاثیر متوسط رو به بالا)\n" +
                               $"🏙 شهرها، پدافند، سربازگیری و دفاع: +{otherPower / 1000.0:F1}K (سایر عوامل)\n\n" +
                               $"ℹ️ مان‌پاور نشان‌دهنده قدرت انسانی و نظامی کشور شماست و به صورت حدودی با واحد K نمایش داده می‌شود.";
            await SendTemp(chat.Id, breakdown, ct: ct);
            return;
        }

        if (txt == "لیست کشور ها" || txt == "لیست کشورها" || txt == "کشور ها" || txt == "کشورها" || txt == "لیست" || txt == "برترین ها" || txt == "رتبه بندی" || txt == "لیدربورد" || txt == "قدرت ها" || txt == "قدرتمندترین ها")
        {
            var allInGroup = Database.GetCountriesByChatId(chat.Id)
                                     .OrderByDescending(c => CalcManpower(c))
                                     .Take(50)
                                     .ToList();
            if (allInGroup.Count == 0)
            {
                await SendTemp(chat.Id, "هنوز هیچ کشوری در این گروه ثبت نشده است.\nبا دستور «انتخاب کشور» اولین کشور را بسازید!", ct: ct);
                return;
            }
            var sb = new StringBuilder("🌍 لیست کشورهای گروه (بر اساس مان‌پاور حدودی):\n\n");
            for (int i = 0; i < allInGroup.Count; i++)
            {
                var c = allInGroup[i];
                string owner = (string.IsNullOrWhiteSpace(c.OwnerName) ? $"کاربر {c.OwnerId}" : c.OwnerName).Trim();
                string shortName = (c.Name.Length > 20 ? c.Name.Substring(0, 20) + "…" : c.Name).Trim();
                double mpK = CalcManpower(c) / 1000.0;
                string prefix = i == 0 ? "🥇 " : (i == 1 ? "🥈 " : (i == 2 ? "🥉 " : $"{i + 1}. "));
                sb.AppendLine($"{prefix}{owner} - {shortName} - {mpK:F1}K");
            }
            await SendTemp(chat.Id, sb.ToString(), ct: ct);
            return;
        }

        if (txt == "ساخت اتحاد" || txt == "ایجاد اتحاد" || txt == "تاسیس اتحاد")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            long curAid = Database.GetUserAllianceId(chat.Id, uid);
            if (curAid > 0)
            {
                await SendTemp(chat.Id, "❌ شما در حال حاضر در یک اتحاد عضو هستید! برای ساخت اتحاد جدید ابتدا با دستور «خروج از اتحاد» یا «انحلال اتحاد» از اتحاد فعلی خارج شوید.", ct: ct);
                return;
            }
            int totalPlayers = Database.GetCountriesByChatId(chat.Id).Count;
            int maxAlliances = Math.Max(1, totalPlayers / 2);
            var alliancesInChat = Database.GetAlliancesByChatId(chat.Id);
            if (alliancesInChat.Count >= maxAlliances)
            {
                await SendTemp(chat.Id, $"⛔ سقف تعداد اتحادهای مجاز در این گروه پر شده است!\n\n👥 تعداد بازیکنان گروه: {totalPlayers} نفر\n🏛 سقف مجاز اتحادها: {maxAlliances} اتحاد (به ازای هر ۲ بازیکن ۱ اتحاد)\n\n💡 برای ساخت اتحاد جدید، یا باید تعداد بازیکنان گروه بیشتر شود و یا یکی از اتحادهای فعلی منحل گردد.", ct: ct);
                return;
            }
            sessions[uid] = new UserSession { Step = SessionStep.WaitingAllianceName, AllianceChatId = chat.Id };
            await SendPrompt(uid, chat.Id, "🏛 نام اتحاد خود را ارسال کنید:", ct: ct);
            return;
        }

        if (txt == "ایجاد درخواست عضویت" || txt == "درخواست عضویت" || txt == "دعوت به اتحاد" || txt == "دعوت")
        {
            var leaderCountry = Database.GetCountry(uid, chat.Id);
            if (leaderCountry == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            long aid = Database.GetUserAllianceId(chat.Id, uid);
            if (aid == 0)
            {
                await SendTemp(chat.Id, "❌ شما در هیچ اتحادی عضو نیستید. ابتدا با دستور «ساخت اتحاد»، اتحاد خود را بسازید.", replyTo: msg.MessageId, ct: ct);
                return;
            }
            var alliance = Database.GetAllianceById(aid);
            if (alliance == null || alliance.LeaderId != uid)
            {
                await SendTemp(chat.Id, "❌ فقط رهبر اتحاد می‌تواند درخواست عضویت ارسال کند!", replyTo: msg.MessageId, ct: ct);
                return;
            }
            if (msg.ReplyToMessage == null || msg.ReplyToMessage.From == null || msg.ReplyToMessage.From.IsBot)
            {
                await SendTemp(chat.Id, "❌ برای ارسال دعوت، باید روی پیام بازیکن مورد نظر ریپلای کنید.", replyTo: msg.MessageId, ct: ct);
                return;
            }
            long tgtId = msg.ReplyToMessage.From.Id;
            if (tgtId == uid) { await SendTemp(chat.Id, "❌ نمی‌توانید خودتان را دعوت کنید!", replyTo: msg.MessageId, ct: ct); return; }
            var tgtCountry = Database.GetCountry(tgtId, chat.Id);
            if (tgtCountry == null) { await SendTemp(chat.Id, "❌ بازیکن مورد نظر در این گپ کشوری ندارد.", replyTo: msg.MessageId, ct: ct); return; }
            if (Database.GetUserAllianceId(chat.Id, tgtId) > 0) { await SendTemp(chat.Id, "❌ این بازیکن در حال حاضر در یک اتحاد دیگر عضو است!", replyTo: msg.MessageId, ct: ct); return; }
            int totPlayers = Database.GetCountriesByChatId(chat.Id).Count;
            int maxMembers = Math.Max(2, totPlayers / 2);
            if (Database.GetAllianceMembers(aid).Count >= maxMembers)
            {
                await SendTemp(chat.Id, $"⛔ ظرفیت اتحاد تکمیل است! سقف: {maxMembers} نفر", replyTo: msg.MessageId, ct: ct);
                return;
            }
            if (IsSuperpowerCollision(chat.Id, uid, tgtId, out string reason))
            {
                await SendTemp(chat.Id, reason, replyTo: msg.MessageId, ct: ct);
                return;
            }
            var inv = new AllianceInvite { AllianceId = aid, ChatId = chat.Id, TargetUserId = tgtId, LeaderId = uid, CreatedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
            long invId = Database.AddAllianceInvite(inv);
            await SendTemp(chat.Id, "✅ درخواست عضویت ارسال شد.", replyTo: msg.MessageId, ct: ct);
            await SendTemp(chat.Id, $"💌 درخواست پیوستن به اتحاد «{alliance.Name}» برای شما ارسال شد، برای تایید یا رد به پیوی مراجعه کنید.", replyTo: msg.ReplyToMessage.MessageId, ct: ct);
            string gTitle = $"گروه {chat.Id}";
            try { var ch = await bot.GetChatAsync(chat.Id, ct); if (!string.IsNullOrEmpty(ch.Title)) gTitle = ch.Title; } catch { }
            var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("✅ تایید و پیوستن", $"ally_accept:{invId}"), InlineKeyboardButton.WithCallbackData("❌ رد درخواست", $"ally_reject:{invId}") } });
            try { await bot.SendTextMessageAsync(tgtId, $"💌 **دعوت‌نامه رسمی اتحاد**\n\n👑 رهبر اتحاد «{alliance.Name}» ({FullName(user)}) در گپ «{gTitle}» از شما دعوت کرده است.", replyMarkup: kb, cancellationToken: ct); }
            catch { await SendTemp(chat.Id, $"⚠️ ارسال پیام به پیوی کاربر {tgtId} ممکن نشد.", replyTo: msg.ReplyToMessage.MessageId, ct: ct); }
            return;
        }

        if (txt == "ترنسفر" || txt == "انتقال" || txt == "ارسال محموله" || txt == "ارسال منابع")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            if (country.PortLevel < 3) { await SendTemp(chat.Id, "⚓ سطح بندر شما برای این عملیات کافی نیست! (حداقل سطح مورد نیاز: ۳)", replyTo: msg.MessageId, ct: ct); return; }
            await SendTemp(chat.Id, "📦 برای ارسال محموله اقتصادی و نظامی به متحدان خود، به پیوی ربات مراجعه کنید.", replyTo: msg.MessageId, ct: ct);
            try
            {
                long aid = Database.GetUserAllianceId(chat.Id, uid);
                if (aid == 0) { await bot.SendTextMessageAsync(uid, "❌ شما در آن گروه عضو هیچ اتحادی نیستید.", cancellationToken: ct); return; }
                var mems = Database.GetAllianceMembers(aid).Where(m => m != uid).ToList();
                if (mems.Count == 0) { await bot.SendTextMessageAsync(uid, "❌ اتحاد شما عضو دیگری ندارد.", cancellationToken: ct); return; }
                if (GetTransferCount(chat.Id, uid) >= MAX_TRANSFERS_PER_UPDATE && !Database.HasGroupLockExemption(chat.Id))
                { await bot.SendTextMessageAsync(uid, $"⛔ سهمیه ترنسفر تمام شد ({MAX_TRANSFERS_PER_UPDATE}).", cancellationToken: ct); return; }
                sessions[uid] = new UserSession { Step = SessionStep.TransferWaitingResource, TransferChatId = chat.Id, TransferAllianceId = aid };
                var kb = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("💰 پول", $"tf_res:{chat.Id}:money"), InlineKeyboardButton.WithCallbackData("🔩 آهن", $"tf_res:{chat.Id}:iron") },
                    new[] { InlineKeyboardButton.WithCallbackData("🪖 سرباز", $"tf_res:{chat.Id}:soldiers"), InlineKeyboardButton.WithCallbackData("🛡 تانک", $"tf_res:{chat.Id}:tanks") },
                    new[] { InlineKeyboardButton.WithCallbackData("✈️ جنگنده", $"tf_res:{chat.Id}:planes"), InlineKeyboardButton.WithCallbackData("🛩 بمب‌افکن", $"tf_res:{chat.Id}:bombers") }
                });
                await bot.SendTextMessageAsync(uid, "📦 **ترنسفر**\n\nنوع منبع را انتخاب کنید:", replyMarkup: kb, cancellationToken: ct);
            }
            catch { }
            return;
        }

        if (txt == "صف آرایی تهاجمی" || txt == "صف آرایی دفاعی" || txt == "صف‌آرایی تهاجمی" || txt == "صف‌آرایی دفاعی")
        {
            bool isOff = txt.Contains("تهاجمی");
            long cid = chat.Id;
            var sc = Database.GetCountry(uid, cid);
            if (sc == null) { await SendTemp(cid, MsgNoCountryGuide, ct: ct); return; }
            if (sc.PortLevel < 3) { await SendTemp(cid, "⚓ سطح بندر شما برای این عملیات کافی نیست! (حداقل سطح مورد نیاز: ۳)", ct: ct); return; }
            long aid = Database.GetUserAllianceId(cid, uid);
            if (aid == 0) { await SendTemp(cid, "❌ فقط اعضای اتحاد می‌توانند صف‌آرایی کنند.", ct: ct); return; }
            var mems = Database.GetAllianceMembers(aid);
            int dailyLimit = mems.Count <= 5 ? 1 : (mems.Count <= 10 ? 2 : (mems.Count <= 20 ? 3 : 5));
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (Database.GetRecentAllianceDeploymentsCount(aid, nowMs - 86400000L) >= dailyLimit && !Database.HasGroupLockExemption(cid))
            { await SendTemp(cid, $"⛔ سقف روزانه صف‌آرایی ({dailyLimit}) پر شد.", ct: ct); return; }
            var tgts = isOff ? Database.GetCountriesByChatId(cid).Where(c => !mems.Contains(c.OwnerId)).ToList() : mems.Select(m => Database.GetCountry(m, cid)).Where(c => c != null).ToList()!;
            if (tgts.Count == 0) { await SendTemp(cid, isOff ? "❌ هیچ هدفی خارج از اتحاد وجود ندارد." : "❌ عضو معتبری برای دفاع وجود ندارد.", ct: ct); return; }
            await SendTemp(cid, "⚔️ برای تنظیم اسکجولر به پی‌وی ربات مراجعه کنید.", replyTo: msg.MessageId, ct: ct);
            var tkb = tgts.Select(t => new[] { InlineKeyboardButton.WithCallbackData($"🏳️ {t!.Name} ({t.OwnerName})", $"dep_target:{cid}:{aid}:{(isOff ? "Off" : "Def")}:{t.OwnerId}") }).ToArray();
            try { await SendPrompt(uid, uid, $"⚔️ **اعلام صف‌آرایی {(isOff ? "تهاجمی" : "دفاعی")}**\n\n🎯 کشور مورد نظر:", new InlineKeyboardMarkup(tkb), ct); } catch { }
            return;
        }

        if (txt == "لغو صف آرایی" || txt == "لغو صف‌آرایی" || txt == "حذف صف آرایی" || txt == "حذف صف‌آرایی")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            long aid = Database.GetUserAllianceId(chat.Id, uid);
            if (aid == 0) { await SendTemp(chat.Id, "❌ شما عضو هیچ اتحادی نیستید.", ct: ct); return; }
            var alliance = Database.GetAllianceById(aid);
            if (alliance == null) return;
            var deps = Database.GetActiveDeployments().Where(d => d.ChatId == chat.Id && d.AllianceId == aid).ToList();
            if (deps.Count == 0) { await SendTemp(chat.Id, "❌ هیچ صف‌آرایی فعالی از اتحاد شما نیست.", ct: ct); return; }
            var myDeps = deps.Where(d => d.InitiatorId == uid || alliance.LeaderId == uid).ToList();
            if (myDeps.Count == 0) { await SendTemp(chat.Id, "❌ شما دسترسی لغو این صف‌آرایی‌ها را ندارید.", ct: ct); return; }
            if (myDeps.Count == 1)
            {
                // FIX(2): ابتدا پیام پین‌شده را آنپین و حذف کن، سپس نیروها را برگردان
                await UnpinAndDeleteAnnounce(myDeps[0].ChatId, myDeps[0].AnnounceMsgId, ct);
                Database.CancelDeploymentForces(myDeps[0]);
                await SendTemp(chat.Id, "🚫 **صف‌آرایی لغو شد!**", ct: ct);
            }
            else
            {
                var kb = myDeps.Select(d => { var tc = Database.GetCountry(d.TargetUserId, chat.Id); string tn = tc?.Name ?? $"کاربر {d.TargetUserId}"; return new[] { InlineKeyboardButton.WithCallbackData($"❌ لغو {(d.Type == "Offensive" ? "حمله" : "دفاع")} {tn}", $"dep_cancel:{d.Id}") }; }).ToArray();
                await SendTemp(chat.Id, "🚫 کدام عملیات لغو شود؟", markup: new InlineKeyboardMarkup(kb), ct: ct);
            }
            return;
        }

        if (txt == "اعزام نیرو" || txt == "مشارکت" || txt == "مشارکت در صف آرایی" || txt == "مشارکت در صف‌آرایی" || txt == "اعزام" || txt == "اعزام نیرو ها" || txt == "اعزام نیروها")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            if (country.PortLevel < 3) { await SendTemp(chat.Id, "⚓ سطح بندر شما برای این عملیات کافی نیست! (حداقل سطح مورد نیاز: ۳)", ct: ct); return; }
            long aid = Database.GetUserAllianceId(chat.Id, uid);
            if (aid == 0) { await SendTemp(chat.Id, "❌ شما عضو هیچ اتحادی نیستید.", ct: ct); return; }
            var deps = Database.GetActiveDeployments().Where(d => d.ChatId == chat.Id && d.AllianceId == aid).ToList();
            if (deps.Count == 0) { await SendTemp(chat.Id, "❌ هیچ صف‌آرایی فعالی از اتحاد شما نیست.", ct: ct); return; }
            var kb = deps.Select(d => { var tc = Database.GetCountry(d.TargetUserId, chat.Id); string tn = tc?.Name ?? $"کاربر {d.TargetUserId}"; return new[] { InlineKeyboardButton.WithCallbackData($"⚔️ {(d.Type == "Offensive" ? "حمله" : "دفاع")} {tn}", $"dep_join:{d.Id}") }; }).ToArray();
            await SendTemp(chat.Id, "⚔️ صف‌آرایی‌های فعال:", markup: new InlineKeyboardMarkup(kb), ct: ct);
            return;
        }

        if (txt == "لیست اتحاد ها" || txt == "لیست اتحادها" || txt == "اتحاد ها" || txt == "اتحادها")
        {
            var alliances = Database.GetAlliancesByChatId(chat.Id);
            if (alliances.Count == 0) { await SendTemp(chat.Id, "هنوز هیچ اتحادی در این گروه نیست.", ct: ct); return; }
            var listWithMp = alliances.Select(a =>
            {
                var members = Database.GetAllianceMembers(a.Id);
                double totalMp = members.Sum(m => { var c = Database.GetCountry(m, chat.Id); return c != null ? CalcManpower(c) : 0; });
                return new { Alliance = a, MembersCount = members.Count, TotalMp = totalMp };
            }).OrderByDescending(x => x.TotalMp).ToList();
            var sb = new StringBuilder("🏆 لیست اتحادهای گروه:\n\n");
            for (int i = 0; i < listWithMp.Count; i++)
            {
                var item = listWithMp[i];
                string prefix = i == 0 ? "🥇 " : (i == 1 ? "🥈 " : (i == 2 ? "🥉 " : $"{i + 1}. "));
                var lc = Database.GetCountry(item.Alliance.LeaderId, chat.Id);
                sb.AppendLine($"{prefix}«{item.Alliance.Name}» — ⚡ {item.TotalMp / 1000.0:F1}K\n   👑 رهبر: {lc?.OwnerName ?? $"کاربر {item.Alliance.LeaderId}"} | 👥 اعضا: {item.MembersCount}");
            }
            await SendTemp(chat.Id, sb.ToString(), ct: ct);
            return;
        }

        if (txt == "وضعیت اتحاد")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            long aid = Database.GetUserAllianceId(chat.Id, uid);
            if (aid == 0) { await SendTemp(chat.Id, "❌ شما در هیچ اتحادی عضو نیستید.", ct: ct); return; }
            var alliance = Database.GetAllianceById(aid);
            if (alliance == null) { await SendTemp(chat.Id, "❌ اطلاعات اتحاد یافت نشد.", ct: ct); return; }
            var memIds = Database.GetAllianceMembers(aid);
            var memCountries = memIds.Select(m => Database.GetCountry(m, chat.Id)).Where(c => c != null).OrderByDescending(c => CalcManpower(c!)).ToList();
            double totalMp = memCountries.Sum(c => CalcManpower(c!));
            var sb = new StringBuilder();
            sb.AppendLine($"🛡 وضعیت اتحاد «{alliance.Name}»");
            sb.AppendLine($"⚡ مان‌پاور کل: {totalMp / 1000.0:F1}K\n👥 رده‌بندی:");
            for (int i = 0; i < memCountries.Count; i++)
            {
                var c = memCountries[i]!;
                string role = c.OwnerId == alliance.LeaderId ? "👑 رهبر" : "👤 عضو";
                string sn = c.Name.Length > 20 ? c.Name.Substring(0, 20) + "…" : c.Name;
                sb.AppendLine($"{i + 1}. {role}: {c.OwnerName} — {sn} — ⚡ {CalcManpower(c) / 1000.0:F1}K");
            }
            if (uid == alliance.LeaderId) { sb.AppendLine("\n💡 برای اخراج: «حذف N» — برای انحلال: «انحلال اتحاد»"); sessions[uid] = new UserSession { Step = SessionStep.LeaderWaitingKickMember, AllianceChatId = chat.Id, AllianceId = alliance.Id }; }
            else { sb.AppendLine("\n💡 برای خروج: «خروج از اتحاد»"); }
            if (!string.IsNullOrEmpty(alliance.FlagFileId)) await SendTempPhoto(chat.Id, alliance.FlagFileId, sb.ToString(), ct: ct);
            else await SendTemp(chat.Id, sb.ToString(), ct: ct);
            return;
        }

        if (txt.StartsWith("حذف "))
        {
            var parts = txt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && TryParseInt(parts[1], out int rank))
            {
                long aid = Database.GetUserAllianceId(chat.Id, uid);
                if (aid > 0)
                {
                    var alliance = Database.GetAllianceById(aid);
                    if (alliance != null && alliance.LeaderId == uid)
                    {
                        var memIds = Database.GetAllianceMembers(alliance.Id);
                        var memCountries = memIds.Select(m => Database.GetCountry(m, chat.Id)).Where(c => c != null).OrderByDescending(c => CalcManpower(c!)).ToList();
                        if (rank == 1) { await SendTemp(chat.Id, "❌ نمی‌توانید خودتان را اخراج کنید!", ct: ct); return; }
                        if (rank < 1 || rank > memCountries.Count) { await SendTemp(chat.Id, $"❌ شماره نامعتبر (۱ تا {memCountries.Count})", ct: ct); return; }
                        var tc = memCountries[rank - 1]!;
                        Database.RemoveAllianceMember(alliance.Id, chat.Id, tc.OwnerId);
                        await SendTemp(chat.Id, $"🚫 {tc.OwnerName} از اتحاد اخراج شد!", ct: ct);
                        try { await bot.SendTextMessageAsync(tc.OwnerId, $"🚫 شما از اتحاد «{alliance.Name}» اخراج شدید.", cancellationToken: ct); } catch { }
                        return;
                    }
                }
            }
        }

        if (txt == "انحلال اتحاد" || txt == "حذف اتحاد")
        {
            long aid = Database.GetUserAllianceId(chat.Id, uid);
            if (aid > 0)
            {
                var alliance = Database.GetAllianceById(aid);
                if (alliance != null && alliance.LeaderId == uid)
                {
                    Database.DeleteAlliance(alliance.Id);
                    if (sessions.ContainsKey(uid)) EndSession(uid);
                    await SendTemp(chat.Id, $"💥 اتحاد «{alliance.Name}» منحل شد!", ct: ct);
                    return;
                }
                else if (alliance != null) { await SendTemp(chat.Id, "❌ فقط رهبر می‌تواند اتحاد را منحل کند.", ct: ct); return; }
            }
        }

        if (txt == "خروج از اتحاد" || txt == "ترک اتحاد")
        {
            long aid = Database.GetUserAllianceId(chat.Id, uid);
            if (aid > 0)
            {
                var alliance = Database.GetAllianceById(aid);
                if (alliance != null && alliance.LeaderId == uid) { await SendTemp(chat.Id, "❌ شما رهبر هستید. از «انحلال اتحاد» استفاده کنید.", ct: ct); return; }
                else if (alliance != null)
                {
                    var c = Database.GetCountry(uid, chat.Id);
                    Database.RemoveAllianceMember(alliance.Id, chat.Id, uid);
                    await SendTemp(chat.Id, $"👋 {c?.OwnerName ?? $"کاربر {uid}"} از اتحاد خارج شد!", ct: ct);
                    try { await bot.SendTextMessageAsync(alliance.LeaderId, $"👋 {c?.OwnerName ?? $"کاربر {uid}"} از اتحاد خارج شد.", cancellationToken: ct); } catch { }
                    return;
                }
            }
        }

        if (txt == "راهنما")
        {
            // FIX(4): راهنمای کامل (در گروه)
            await SendTemp(chat.Id, HelpText, parseMode: ParseMode.Html, ct: ct);
            return;
        }

        if (txt == "خرید تانک" || txt == "ساخت تانک")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            InlineKeyboardMarkup tk = country.Faction switch
            {
                Faction.USA => new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🇺🇸 M2 Medium", $"tank_info:{uid}:M2Medium") } }),
                Faction.USSR => new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 T-28", $"tank_info:{uid}:T28") } }),
                _ => new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🇩🇪 Panzer III", $"tank_info:{uid}:PanzerIII") } })
            };
            await SendTemp(chat.Id, "🛡️ تانک:", markup: tk, ct: ct);
            return;
        }

        if (txt == "خرید هواپیما" || txt == "ساخت هواپیما" || txt == "خرید جنگنده" || txt == "ساخت جنگنده")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            var fb = country.Faction switch
            {
                Faction.USA => new[] { InlineKeyboardButton.WithCallbackData("🇺🇸 P-36", $"plane_info:{uid}:P36"), InlineKeyboardButton.WithCallbackData("🇺🇸 B-17", $"bomber_info:{uid}:B17") },
                Faction.USSR => new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 I-16", $"plane_info:{uid}:I16"), InlineKeyboardButton.WithCallbackData("🇷🇺 DB-3", $"bomber_info:{uid}:DB3") },
                _ => new[] { InlineKeyboardButton.WithCallbackData("🇩🇪 Bf 109", $"plane_info:{uid}:Bf109"), InlineKeyboardButton.WithCallbackData("🇩🇪 He 111", $"bomber_info:{uid}:He111") }
            };
            await SendTemp(chat.Id, "🛩️ نیروی هوایی:", markup: new InlineKeyboardMarkup(new[] { new[] { fb[0] }, new[] { fb[1] } }), ct: ct);
            return;
        }

        if (txt == "خرید بمب افکن" || txt == "ساخت بمب افکن" || txt == "خرید بمب‌افکن" || txt == "ساخت بمب‌افکن")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            var bk = country.Faction switch
            {
                Faction.USA => new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🇺🇸 B-17", $"bomber_info:{uid}:B17") } }),
                Faction.USSR => new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🇷🇺 DB-3", $"bomber_info:{uid}:DB3") } }),
                _ => new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🇩🇪 He 111", $"bomber_info:{uid}:He111") } })
            };
            await SendTemp(chat.Id, "🛩️ بمب‌افکن:", markup: bk, ct: ct);
            return;
        }

        if (txt == "پدافند" || txt == "خرید پدافند" || txt == "ساخت پدافند" || txt == "ضدهوایی" || txt == "ضد هوایی" || txt == "خرید ضد هوایی" || txt == "ساخت ضد هوایی")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            await SendTemp(chat.Id, "🎯 پدافند:", markup: new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🎯 توپ ۷۶ میلی‌متری", $"aa_info:{uid}:AA76") } }), ct: ct);
            return;
        }

        if (txt == "تغییر اسم" || txt == "تعویض اسم" || txt == "تغییر اسم کشور" || txt == "تعویض اسم کشور")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            sessions[uid] = new UserSession { Step = SessionStep.WaitingNewName, ChatId = chat.Id };
            await SendPrompt(uid, chat.Id, "اسم جدید را ارسال کنید.", ct: ct);
            return;
        }

        if (txt == "ترید")
        {
            long r = Database.GetRoyalCoins(uid);
            sessions[uid] = new UserSession { Step = SessionStep.WaitingTradeAmount, ChatId = chat.Id };
            await SendPrompt(uid, chat.Id, $"💎 رویال: {r}\n\nچند رویال تبدیل کنید? (هر رویال = 10K)", ct: ct);
            return;
        }

        if (txt == "آموزش سرباز" || txt == "نرخ سرباز گیری" || txt == "نرخ سربازگیری")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            sessions[uid] = new UserSession { Step = SessionStep.WaitingRecruitmentRate, ChatId = chat.Id };
            await SendPrompt(uid, chat.Id, $"🎯 نرخ فعلی: {country.RecruitmentRate}\nعدد 0 تا 10:", ct: ct);
            return;
        }

        if (txt == "مالیات" || txt == "نرخ مالیات")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            sessions[uid] = new UserSession { Step = SessionStep.WaitingTaxRate, ChatId = chat.Id };
            long est = CalcTaxIncome(country);
            await SendPrompt(uid, chat.Id, $"💰 نرخ فعلی: {country.TaxRate}%\n📈 برآورد: {est / 1000.0:F1}K\nعدد 0 تا 100:", ct: ct);
            return;
        }

        if (txt == "تغییر پرچم" || txt == "تعویض پرچم" || txt == "پرچم")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            sessions[uid] = new UserSession { Step = SessionStep.WaitingNewFlag, ChatId = chat.Id };
            await SendPrompt(uid, chat.Id, "عکس پرچم جدید را ارسال کنید.", ct: ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var renameSess) && renameSess != null && renameSess.Step == SessionStep.WaitingNewName)
        {
            if (string.IsNullOrWhiteSpace(txt)) { await SendPrompt(uid, chat.Id, "اسم معتبر بفرستید.", ct: ct); return; }
            if (Database.CountryNameExists(txt)) { await SendPrompt(uid, chat.Id, "این اسم قبلاً استفاده شده.", ct: ct); return; }
            Database.UpdateCountryName(uid, chat.Id, txt);
            EndSession(uid);
            await SendTemp(chat.Id, $"✅ نام کشور به {txt} تغییر یافت.", ct: ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var recSess) && recSess != null && recSess.Step == SessionStep.WaitingRecruitmentRate)
        {
            if (!TryParseInt(txt, out int rate) || rate < 0 || rate > 10) { await SendPrompt(uid, chat.Id, "❌ عدد 0 تا 10:", ct: ct); return; }
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { EndSession(uid); return; }
            country.RecruitmentRate = rate;
            Database.UpdateCountryFull(country);
            EndSession(uid);
            await SendTemp(chat.Id, $"✅ نرخ سربازگیری: {rate}\n🏥 هدف رفاه: {WelfareTarget(country):F0}%", ct: ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var taxSess) && taxSess != null && taxSess.Step == SessionStep.WaitingTaxRate)
        {
            if (!TryParseInt(txt, out int tx) || tx < 0 || tx > 100) { await SendPrompt(uid, chat.Id, "❌ عدد 0 تا 100:", ct: ct); return; }
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { EndSession(uid); return; }
            country.TaxRate = tx;
            Database.UpdateCountryFull(country);
            EndSession(uid);
            long est = CalcTaxIncome(country);
            await SendTemp(chat.Id, $"✅ نرخ مالیات: {tx}%\n💰 برآورد: {est / 1000.0:F1}K\n🏥 هدف رفاه: {WelfareTarget(country):F0}%", ct: ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var tradeSess) && tradeSess != null && tradeSess.Step == SessionStep.WaitingTradeAmount)
        {
            if (!TryParseLong(txt, out long ta) || ta <= 0) { await SendPrompt(uid, chat.Id, "عدد معتبر.", ct: ct); return; }
            long r = Database.GetRoyalCoins(uid);
            if (ta > r) { await SendPrompt(uid, chat.Id, $"رویال کافی نیست. موجودی: {r}", ct: ct); return; }
            var ctry = Database.GetCountry(uid, chat.Id);
            if (ctry == null) { EndSession(uid); return; }
            Database.AddRoyalCoins(uid, -ta);
            ctry.Money += ta * 10000L;
            Database.UpdateCountryResources(uid, chat.Id, ctry.Money, ctry.Iron, ctry.Tanks);
            EndSession(uid);
            await SendPermanent(chat.Id, $"✅ {ta} رویال تبدیل شد 💰 +{ta * 10}K", ct: ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var flagSess) && flagSess != null && flagSess.Step == SessionStep.WaitingNewFlag)
        {
            if (msg.Photo == null || msg.Photo.Length == 0) { await SendPrompt(uid, chat.Id, "لطفاً عکس ارسال کنید.", ct: ct); return; }
            string fid = msg.Photo.Last().FileId;
            Database.UpdateCountryFlag(uid, chat.Id, fid);
            EndSession(uid);
            await SendTemp(chat.Id, "✅ پرچم تغییر کرد.", ct: ct);
            var country = Database.GetCountry(uid, chat.Id);
            if (country != null) await SendCountryInfo(chat.Id, country, ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var allyFlagSess) && allyFlagSess != null && allyFlagSess.Step == SessionStep.WaitingAllianceFlag)
        {
            if (msg.Photo == null || msg.Photo.Length == 0) { await SendPrompt(uid, chat.Id, "عکس ارسال کنید.", ct: ct); return; }
            string fid = msg.Photo.Last().FileId;
            var al = new Alliance { ChatId = allyFlagSess.AllianceChatId, Name = allyFlagSess.AllianceName, FlagFileId = fid, LeaderId = uid, CreatedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
            long aid = Database.AddAlliance(al);
            EndSession(uid);
            await SendTemp(chat.Id, $"🎉 اتحاد «{al.Name}» تاسیس شد!\n👑 رهبر: {FullName(user)}", ct: ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var allyNameSess) && allyNameSess != null && allyNameSess.Step == SessionStep.WaitingAllianceName)
        {
            if (string.IsNullOrWhiteSpace(txt)) { await SendPrompt(uid, chat.Id, "نام معتبر.", ct: ct); return; }
            if (Database.AllianceNameExists(chat.Id, txt)) { await SendPrompt(uid, chat.Id, "این نام قبلاً ثبت شده.", ct: ct); return; }
            allyNameSess.Step = SessionStep.WaitingAllianceFlag;
            allyNameSess.AllianceName = txt;
            await SendPrompt(uid, chat.Id, $"✅ نام: «{txt}»\n🚩 عکس پرچم را ارسال کنید:", ct: ct);
            return;
        }

        if (sessions.TryGetValue(uid, out var sess) && sess != null && sess.Step == SessionStep.WaitingCountryName)
        {
            long rm = Database.GetLeaveCooldownRemainingMs(uid, chat.Id);
            if (rm > 0) { EndSession(uid); await SendTemp(chat.Id, $"⛔ تا {FormatRemaining(rm)} نمی‌توانید کشور بسازید.", ct: ct); return; }
            if (string.IsNullOrWhiteSpace(txt)) { await SendPrompt(uid, chat.Id, "اسم معتبر.", ct: ct); return; }
            if (Database.CountryNameExists(txt)) { await SendPrompt(uid, chat.Id, "این اسم استفاده شده.", ct: ct); return; }
            var flags = Database.GetFactionFlags(sess.FactionStr);
            string flagId = flags.Count > 0 ? flags[rng.Next(flags.Count)] : "";
            var nc = new Country
            {
                ChatId = chat.Id, Name = txt, OwnerId = uid, OwnerName = FullName(user),
                Faction = sess.Faction, FlagFileId = flagId,
                Money = 10000, Population = 100000, FactoryLevel = 1, PortLevel = 1, MineLevel = 1, Iron = 0,
                CreatedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            try { Database.AddCountry(nc); } catch (Exception ex) { Console.WriteLine($"[AddCountry FAIL] {ex.Message}"); EndSession(uid); return; }
            EndSession(uid);
            await SendCountryInfo(chat.Id, nc, ct);
            return;
        }

        if (txt == "حمله")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            await SendTemp(chat.Id, "⚔️ برای مشخص کردن هدف به پیوی مراجعه کنید.", replyTo: msg.MessageId, ct: ct);
            var targets = Database.GetCountriesByChatId(chat.Id).Where(c => c.OwnerId != uid).ToList();
            if (targets.Count == 0) { await SendTemp(uid, "هیچ هدفی در این گروه وجود ندارد.", ct: ct); return; }
            var kb = targets.Select(t => new[] { InlineKeyboardButton.WithCallbackData(t.OwnerName, $"attack_target:{chat.Id}:{t.OwnerId}") }).ToArray();
            sessions[uid] = new UserSession { Step = SessionStep.AttackWaitingTarget, AttackChatId = chat.Id };
            await SendPrompt(uid, uid, "🎯 هدف را انتخاب کنید:", new InlineKeyboardMarkup(kb), ct);
            return;
        }

        if (txt == "وضعیت دفاع")
        {
            var country = Database.GetCountry(uid, chat.Id);
            if (country == null) { await SendTemp(chat.Id, MsgNoCountryGuide, ct: ct); return; }
            await SendTemp(chat.Id, "🛡 وضعیت دفاع به پیوی ارسال شد.", replyTo: msg.MessageId, ct: ct);
            try { await SendDefenseStatus(uid, uid, chat.Id, ct); }
            catch { await SendTemp(chat.Id, "⚠️ ابتدا ربات را در پیوی استارت کنید.", replyTo: msg.MessageId, ct: ct); }
            return;
        }
    }

    // ============================================================
    //  Private message handlers
    // ============================================================
    static async Task SendStartMessage(long uid, CancellationToken ct)
    {
        // FIX(3): پیام خوش‌آمد/استارت در پیوی — تا کاربر فکر نکند بات خاموش است
        string startText =
            "👋 سلام! ربات «آلیس» روشن و فعال است ✅\n\n" +
            "🎮 این یک بازی استراتژیک جنگ جهانی است که <b>فقط داخل گروه‌ها</b> اجرا می‌شود.\n" +
            "برای بازی، ربات را به گروه خود اضافه کنید و در همان‌جا دستورها را بنویسید.\n\n" +
            "📌 دستورهای بازی (مثل «انتخاب کشور»، «دارایی»، «حمله» و ...) را باید <b>در گروه</b> بفرستید؛ " +
            "بعضی مراحل (حمله، ترنسفر، صف‌آرایی، وضعیت دفاع) به‌طور خودکار برای تنظیم دقیق به همین پیوی هدایت می‌شوند.\n\n" +
            "ℹ️ برای دیدن فهرست کامل دستورها و توضیح هرکدام، همین‌جا در پیوی بنویسید: <b>راهنما</b>\n" +
            "(دستور «راهنما» هم در گروه و هم در پیوی کار می‌کند.)";
        await SendPermanent(uid, startText, parseMode: ParseMode.Html, ct: ct);
    }

    static async Task HandleUserPrivateAsync(Message msg, User user, CancellationToken ct)
    {
        long uid = user.Id;
        string txt = msg.Text?.Trim() ?? "";

        if (txt == "لغو")
        {
            var attackStates = new[] {
                SessionStep.AttackWaitingGroup, SessionStep.AttackWaitingTarget,
                SessionStep.AttackWaitingStrategy, SessionStep.AttackWaitingTactic,
                SessionStep.AttackWaitingTanks, SessionStep.AttackWaitingSoldiers,
                SessionStep.AttackWaitingFighters, SessionStep.AttackWaitingBombers,
                SessionStep.AttackWaitingAirStrategy, SessionStep.AttackWaitingAirTactic
            };
            if (sessions.TryGetValue(uid, out var cancelSess) && cancelSess != null &&
                attackStates.Contains(cancelSess.Step))
            {
                EndSession(uid);
                await SendTemp(uid, "✅ انصراف از حمله ثبت شد (بدون قفل جریمه).", ct: ct);
                return;
            }
            else
            {
                EndSession(uid);
                await SendTemp(uid, "✅ عملیات لغو شد.", ct: ct);
            }
            return;
        }

        if (sessions.TryGetValue(uid, out var sess) && sess != null)
        {
            if (sess.Step == SessionStep.TransferWaitingAmount)
            {
                if (!TryParseLong(txt, out long amount) || amount <= 0) { await SendPrompt(uid, uid, "❌ عدد مثبت:", ct: ct); return; }
                var c = Database.GetCountry(uid, sess.TransferChatId);
                if (c == null) { EndSession(uid); return; }
                long myAid = Database.GetUserAllianceId(sess.TransferChatId, uid);
                if (myAid == 0) { EndSession(uid); await SendTemp(uid, "❌ شما دیگر عضو اتحاد نیستید.", ct: ct); return; }
                long tgtAid = Database.GetUserAllianceId(sess.TransferChatId, sess.TransferTargetId);
                if (tgtAid != myAid) { EndSession(uid); await SendTemp(uid, "❌ گیرنده هم‌اتحاد شما نیست.", ct: ct); return; }
                var recv = Database.GetCountry(sess.TransferTargetId, sess.TransferChatId);
                if (recv == null) { EndSession(uid); await SendTemp(uid, "❌ گیرنده کشوری ندارد.", ct: ct); return; }
                if (GetTransferCount(sess.TransferChatId, uid) >= MAX_TRANSFERS_PER_UPDATE && !Database.HasGroupLockExemption(sess.TransferChatId))
                { EndSession(uid); await SendTemp(uid, $"⛔ سهمیه تمام شد ({MAX_TRANSFERS_PER_UPDATE}).", ct: ct); return; }
                long avail = sess.TransferResourceType switch { "money" => c.Money, "iron" => c.Iron, "soldiers" => c.Soldiers, "tanks" => c.Tanks, "planes" => c.Planes, _ => c.Bombers };
                string resName = GetResName(sess.TransferResourceType);
                if (amount > avail) { await SendPrompt(uid, uid, $"❌ موجودی: {avail:N0}", ct: ct); return; }
                switch (sess.TransferResourceType)
                {
                    case "money": c.Money -= amount; break;
                    case "iron": c.Iron -= amount; break;
                    case "soldiers": c.Soldiers -= amount; break;
                    case "tanks": c.Tanks -= amount; break;
                    case "planes": c.Planes -= amount; break;
                    case "bombers": c.Bombers -= amount; break;
                }
                Database.UpdateCountryFull(c);
                Database.ReconcileDefense(uid, sess.TransferChatId);
                bool isTfExempt = Database.HasGroupLockExemption(sess.TransferChatId); long arrMs = isTfExempt ? 0 : DateTimeOffset.UtcNow.AddMinutes(sess.TransferDurationMin).ToUnixTimeMilliseconds();
                var tf = new Transfer { ChatId = sess.TransferChatId, AllianceId = myAid, SenderId = uid, ReceiverId = sess.TransferTargetId, ResourceType = sess.TransferResourceType, Amount = amount, ArriveAtMs = arrMs, Notified = 0 };
                Database.AddTransfer(tf);
                IncTransferCount(sess.TransferChatId, uid);
                EndSession(uid);
                if (isTfExempt) { await SendTemp(uid, $"✅ محموله ارسال شد!\n📦 {amount:N0} {resName}\n" + "⚡ تحویل فوری (معافیت کامل گروه)", ct: ct); _ = Task.Run(async () => { try { await ProcessActiveTransfers(CancellationToken.None); } catch { } }); } else { await SendTemp(uid, $"✅ محموله ارسال شد!\n📦 {amount:N0} {resName}\n⏳ {sess.TransferDurationMin} دقیقه دیگر", ct: ct); }
                try { await bot.SendTextMessageAsync(sess.TransferTargetId, $"🚚 محموله از {c.OwnerName} ({c.Name}): {amount:N0} {resName} — {sess.TransferDurationMin} دقیقه دیگر", cancellationToken: ct); } catch { }
                return;
            }

            if (sess.Step == SessionStep.DeployWaitingTanks)
            {
                if (!TryParseLong(txt, out long tnk) || tnk < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var c = Database.GetCountry(uid, sess.DeployChatId);
                if (c == null) { EndSession(uid); return; }
                if (tnk > c.Tanks) { await SendPrompt(uid, uid, $"❌ موجودی: {c.Tanks}", ct: ct); return; }
                sess.DeployTanks = tnk;
                sess.Step = SessionStep.DeployWaitingSoldiers;
                await SendPrompt(uid, uid, $"🪖 سرباز:\nموجود: {c.Soldiers:N0}", ct: ct);
                return;
            }
            if (sess.Step == SessionStep.DeployWaitingSoldiers)
            {
                if (!TryParseLong(txt, out long sol) || sol < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var c = Database.GetCountry(uid, sess.DeployChatId);
                if (c == null) { EndSession(uid); return; }
                if (sol > c.Soldiers) { await SendPrompt(uid, uid, $"❌ موجودی: {c.Soldiers}", ct: ct); return; }
                sess.DeploySoldiers = sol;
                sess.Step = SessionStep.DeployWaitingFighters;
                await SendPrompt(uid, uid, $"✈️ جنگنده:\nموجود: {c.Planes:N0}", ct: ct);
                return;
            }
            if (sess.Step == SessionStep.DeployWaitingFighters)
            {
                if (!TryParseLong(txt, out long fig) || fig < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var c = Database.GetCountry(uid, sess.DeployChatId);
                if (c == null) { EndSession(uid); return; }
                if (fig > c.Planes) { await SendPrompt(uid, uid, $"❌ موجودی: {c.Planes}", ct: ct); return; }
                sess.DeployFighters = fig;
                sess.Step = SessionStep.DeployWaitingBombers;
                await SendPrompt(uid, uid, $"🛩 بمب‌افکن:\nموجود: {c.Bombers:N0}", ct: ct);
                return;
            }
            if (sess.Step == SessionStep.DeployWaitingBombers)
            {
                if (!TryParseLong(txt, out long bom) || bom < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var c = Database.GetCountry(uid, sess.DeployChatId);
                if (c == null) { EndSession(uid); return; }
                if (bom > c.Bombers) { await SendPrompt(uid, uid, $"❌ موجودی: {c.Bombers}", ct: ct); return; }
                sess.DeployBombers = bom;
                long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long endMs = nowMs + sess.DeployDuration * 3600000L;
                var dep = new Deployment
                {
                    ChatId = sess.DeployChatId, AllianceId = sess.DeployAllianceId, InitiatorId = uid, TargetUserId = sess.DeployTargetId,
                    Type = sess.DeployType, DurationHours = sess.DeployDuration, FormationType = sess.DeployFormation,
                    Strategy = sess.DeployStrategy, Tactic = sess.DeployTactic,
                    Tanks = sess.DeployTanks, Soldiers = sess.DeploySoldiers, Fighters = sess.DeployFighters, Bombers = sess.DeployBombers,
                    CreatedAtMs = nowMs, EndAtMs = endMs, LastWarnMs = nowMs
                };
                c.Tanks -= sess.DeployTanks; c.Soldiers -= sess.DeploySoldiers; c.Planes -= sess.DeployFighters; c.Bombers -= sess.DeployBombers;
                Database.UpdateCountryFull(c);
                Database.ReconcileDefense(uid, sess.DeployChatId);
                long depId = Database.AddDeployment(dep);
                var initC = new DeploymentContributor { DeploymentId = depId, UserId = uid, Tanks = sess.DeployTanks, Soldiers = sess.DeploySoldiers, Fighters = sess.DeployFighters, Bombers = sess.DeployBombers, Strategy = sess.DeployStrategy, Tactic = sess.DeployTactic };
                Database.AddDeploymentContributor(initC);
                if (sess.DeployType == "Defensive")
                {
                    var tcDef = Database.GetCountry(sess.DeployTargetId, sess.DeployChatId);
                    if (tcDef != null)
                    {
                        tcDef.Tanks += sess.DeployTanks; tcDef.Soldiers += sess.DeploySoldiers; tcDef.Planes += sess.DeployFighters; tcDef.Bombers += sess.DeployBombers;
                        tcDef.DefenseTanks += sess.DeployTanks; tcDef.DefenseSoldiers += sess.DeploySoldiers; tcDef.DefenseFighters += sess.DeployFighters;
                        Database.UpdateCountryFull(tcDef);
                        Database.ReconcileDefense(tcDef.OwnerId, tcDef.ChatId);
                    }
                }
                EndSession(uid);
                var alliance = Database.GetAllianceById(sess.DeployAllianceId);
                string allyName = alliance?.Name ?? "اتحاد";
                var tc = Database.GetCountry(sess.DeployTargetId, sess.DeployChatId);
                string tName = tc?.Name ?? $"کاربر {sess.DeployTargetId}";
                var memIds = Database.GetAllianceMembers(sess.DeployAllianceId);
                var memC = memIds.Select(m => Database.GetCountry(m, sess.DeployChatId)).Where(x => x != null).ToList();
                string tags = string.Join(" ", memC.Select(x => HtmlTag(x!.OwnerName, x!.OwnerId)));
                string targetTag = tc != null ? HtmlTag(tc.OwnerName, tc.OwnerId) : $"کاربر {sess.DeployTargetId}";
                bool isOff = sess.DeployType == "Offensive";
                bool isUni = sess.DeployFormation == "Unified";
                string bText = isOff ?
                    $"🚨 <b>اعلان جنگ و صف‌آرایی تهاجمی!</b> ⚔️\n\n👑 اتحاد <b>«{allyName}»</b> علیه کشور <b>«{tName}»</b> (مالک: {targetTag}) صف‌آرایی کرد!\n🎯 نوع آرایش: <b>{(isUni ? "یکپارچه" : "چندجبهه‌ای")}</b>\n⏱ مدت: <b>{sess.DeployDuration} ساعت</b> (پایان: {FormatTime(endMs)})\n\n💥 <b>نیروهای اولیه:</b>\n🪖 سرباز: {sess.DeploySoldiers:N0} | 🛡 تانک: {sess.DeployTanks:N0}\n✈️ جنگنده: {sess.DeployFighters:N0} | 🛩 بمب‌افکن: {sess.DeployBombers:N0}\n\n👥 اعضای اتحاد:\n{tags}" :
                    $"🛡 <b>اعلام صف‌آرایی دفاعی!</b> 🏰\n\n👑 اتحاد <b>«{allyName}»</b> برای حمایت از کشور <b>«{tName}»</b> (مالک: {targetTag}) خط پدافندی تشکیل داد!\n🎯 نوع آرایش: <b>{(isUni ? "یکپارچه" : "چندجبهه‌ای")}</b>\n⏱ مدت: <b>{sess.DeployDuration} ساعت</b> (پایان: {FormatTime(endMs)})\n\n🛡 <b>نیروهای پشتیبان:</b>\n🪖 سرباز: {sess.DeploySoldiers:N0} | 🛡 تانک: {sess.DeployTanks:N0}\n✈️ جنگنده: {sess.DeployFighters:N0} | 🛩 بمب‌افکن: {sess.DeployBombers:N0}\n\n👥 اعضای اتحاد:\n{tags}";
                string fCat = isOff ? "OffensiveDeploy" : "DefensiveDeploy";
                var photos = Database.GetFactionFlags(fCat);
                // FIX(1): دکمهٔ صحیح (dep_join) — قبلاً depjoin بود و کار نمی‌کرد
                var joinKb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⚔️ مشارکت و اعزام نیرو", $"dep_join:{depId}") } });
                Message depMsg;
                if (photos.Count > 0)
                {
                    string rPhoto = photos[rng.Next(photos.Count)];
                    try { depMsg = await bot.SendPhotoAsync(sess.DeployChatId, new InputOnlineFile(rPhoto), caption: bText, parseMode: ParseMode.Html, replyMarkup: joinKb, cancellationToken: ct); }
                    catch { depMsg = await bot.SendTextMessageAsync(sess.DeployChatId, bText, parseMode: ParseMode.Html, replyMarkup: joinKb, cancellationToken: ct); }
                }
                else { depMsg = await bot.SendTextMessageAsync(sess.DeployChatId, bText, parseMode: ParseMode.Html, replyMarkup: joinKb, cancellationToken: ct); }
                // FIX(2): MessageId پیام اعلام را ذخیره کن تا هنگام لغو/پایان آنپین و حذف شود
                try { Database.UpdateDeploymentAnnounceMsg(depId, depMsg.MessageId); } catch { }
                try { await bot.PinChatMessageAsync(sess.DeployChatId, depMsg.MessageId, disableNotification: false, cancellationToken: ct); } catch { }
                await SendTemp(uid, "✅ صف‌آرایی ثبت و در گروه اعلام شد.", ct: ct);
                if (isOff)
                {
                    string gTitle = $"گروه {sess.DeployChatId}";
                    try { var ch = await bot.GetChatAsync(sess.DeployChatId, ct); if (!string.IsNullOrEmpty(ch.Title)) gTitle = ch.Title; } catch { }
                    try { await bot.SendTextMessageAsync(sess.DeployTargetId, $"⚠️ هشدار: صف‌آرایی تهاجمی علیه شما در «{gTitle}»!\n⏱ {sess.DeployDuration} ساعت دیگر", cancellationToken: ct); } catch { }
                }
                return;
            }

            // DeployJoin steps
            if (sess.Step == SessionStep.DeployJoinWaitingTanks)
            {
                if (!TryParseLong(txt, out long tnk) || tnk < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var c = Database.GetCountry(uid, sess.DeployChatId);
                if (c == null) { EndSession(uid); return; }
                if (tnk > c.Tanks) { await SendPrompt(uid, uid, $"❌ موجودی: {c.Tanks}", ct: ct); return; }
                sess.DeployJoinTanks = tnk;
                sess.Step = SessionStep.DeployJoinWaitingSoldiers;
                await SendPrompt(uid, uid, $"🪖 سرباز:\nموجود: {c.Soldiers:N0}", ct: ct);
                return;
            }
            if (sess.Step == SessionStep.DeployJoinWaitingSoldiers)
            {
                if (!TryParseLong(txt, out long sol) || sol < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var c = Database.GetCountry(uid, sess.DeployChatId);
                if (c == null) { EndSession(uid); return; }
                if (sol > c.Soldiers) { await SendPrompt(uid, uid, $"❌ موجودی: {c.Soldiers}", ct: ct); return; }
                sess.DeployJoinSoldiers = sol;
                sess.Step = SessionStep.DeployJoinWaitingFighters;
                await SendPrompt(uid, uid, $"✈️ جنگنده:\nموجود: {c.Planes:N0}", ct: ct);
                return;
            }
            if (sess.Step == SessionStep.DeployJoinWaitingFighters)
            {
                if (!TryParseLong(txt, out long fig) || fig < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var c = Database.GetCountry(uid, sess.DeployChatId);
                if (c == null) { EndSession(uid); return; }
                if (fig > c.Planes) { await SendPrompt(uid, uid, $"❌ موجودی: {c.Planes}", ct: ct); return; }
                sess.DeployJoinFighters = fig;
                sess.Step = SessionStep.DeployJoinWaitingBombers;
                await SendPrompt(uid, uid, $"🛩 بمب‌افکن:\nموجود: {c.Bombers:N0}", ct: ct);
                return;
            }
            if (sess.Step == SessionStep.DeployJoinWaitingBombers)
            {
                if (!TryParseLong(txt, out long bom) || bom < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var c = Database.GetCountry(uid, sess.DeployChatId);
                if (c == null) { EndSession(uid); return; }
                if (bom > c.Bombers) { await SendPrompt(uid, uid, $"❌ موجودی: {c.Bombers}", ct: ct); return; }
                sess.DeployJoinBombers = bom;
                var dep = Database.GetDeploymentById(sess.DeployJoinId);
                if (dep == null || dep.EndAtMs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) { EndSession(uid); await SendTemp(uid, "❌ مهلت پایان یافته.", ct: ct); return; }
                c.Tanks -= sess.DeployJoinTanks; c.Soldiers -= sess.DeployJoinSoldiers; c.Planes -= sess.DeployJoinFighters; c.Bombers -= sess.DeployJoinBombers;
                Database.UpdateCountryFull(c);
                Database.ReconcileDefense(uid, sess.DeployChatId);
                dep.Tanks += sess.DeployJoinTanks; dep.Soldiers += sess.DeployJoinSoldiers; dep.Fighters += sess.DeployJoinFighters; dep.Bombers += sess.DeployJoinBombers;
                Database.UpdateDeploymentForces(dep);
                var contrib = new DeploymentContributor { DeploymentId = dep.Id, UserId = uid, Tanks = sess.DeployJoinTanks, Soldiers = sess.DeployJoinSoldiers, Fighters = sess.DeployJoinFighters, Bombers = sess.DeployJoinBombers, Strategy = sess.DeployJoinStrategy, Tactic = sess.DeployJoinTactic };
                Database.AddDeploymentContributor(contrib);
                if (dep.Type == "Defensive")
                {
                    var tcDef = Database.GetCountry(dep.TargetUserId, dep.ChatId);
                    if (tcDef != null)
                    {
                        tcDef.Tanks += sess.DeployJoinTanks; tcDef.Soldiers += sess.DeployJoinSoldiers; tcDef.Planes += sess.DeployJoinFighters; tcDef.Bombers += sess.DeployJoinBombers;
                        tcDef.DefenseTanks += sess.DeployJoinTanks; tcDef.DefenseSoldiers += sess.DeployJoinSoldiers; tcDef.DefenseFighters += sess.DeployJoinFighters;
                        Database.UpdateCountryFull(tcDef);
                        Database.ReconcileDefense(tcDef.OwnerId, tcDef.ChatId);
                    }
                }
                EndSession(uid);
                await SendTemp(uid, "✅ نیروها اعزام شدند!", ct: ct);
                string announce = $"🚀 کشور «{c.Name}» ({c.OwnerName}) نیروی کمکی اعزام کرد: {sess.DeployJoinTanks:N0} تانک, {sess.DeployJoinSoldiers:N0} سرباز, {sess.DeployJoinFighters:N0} جنگنده, {sess.DeployJoinBombers:N0} بمب‌افکن";
                try { await SendPermanent(dep.ChatId, announce, ct: ct); } catch { }
                return;
            }

            // Attack steps
            if (sess.Step == SessionStep.AttackWaitingTanks)
            {
                if (!TryParseLong(txt, out long tanks) || tanks < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var atk = Database.GetCountry(uid, sess.AttackChatId);
                if (atk == null) { EndSession(uid); return; }
                if (tanks > atk.Tanks) { await SendPrompt(uid, uid, $"❌ موجودی: {atk.Tanks}", ct: ct); return; }
                sess.AttackTanks = tanks;
                sess.Step = SessionStep.AttackWaitingSoldiers;
            await SendPrompt(uid, uid, "🪖 تعداد سربازان اعزامی را وارد کنید.\n" + InventoryLine(atk.Soldiers), ct: ct);
                return;
            }
            if (sess.Step == SessionStep.AttackWaitingSoldiers)
            {
                if (!TryParseLong(txt, out long soldiers) || soldiers < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var atk = Database.GetCountry(uid, sess.AttackChatId);
                if (atk == null) { EndSession(uid); return; }
                if (soldiers > atk.Soldiers) { await SendPrompt(uid, uid, $"❌ موجودی: {atk.Soldiers}", ct: ct); return; }
                sess.AttackSoldiers = soldiers;
                sess.Step = SessionStep.AttackWaitingFighters;
            await SendPrompt(uid, uid, "✈️ تعداد جنگنده‌های اعزامی را وارد کنید.\n" + InventoryLine(atk.Planes), ct: ct);
                return;
            }
            if (sess.Step == SessionStep.AttackWaitingFighters)
            {
                if (!TryParseLong(txt, out long fighters) || fighters < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var atk = Database.GetCountry(uid, sess.AttackChatId);
                if (atk == null) { EndSession(uid); return; }
                if (fighters > atk.Planes) { await SendPrompt(uid, uid, $"❌ موجودی: {atk.Planes}", ct: ct); return; }
                sess.AttackFighters = fighters;
                sess.Step = SessionStep.AttackWaitingBombers;
            await SendPrompt(uid, uid, "🛩 تعداد بمب‌افکن‌های اعزامی را وارد کنید.\n" + InventoryLine(atk.Bombers), ct: ct);
                return;
            }
            if (sess.Step == SessionStep.AttackWaitingBombers)
            {
                if (!TryParseLong(txt, out long bombers) || bombers < 0) { await SendPrompt(uid, uid, "❌ عدد معتبر.", ct: ct); return; }
                var atk = Database.GetCountry(uid, sess.AttackChatId);
                if (atk == null) { EndSession(uid); return; }
                if (bombers > atk.Bombers) { await SendPrompt(uid, uid, $"❌ موجودی: {atk.Bombers}", ct: ct); return; }
                sess.AttackBombers = bombers;
                if (sess.AttackFighters == 0 && sess.AttackBombers == 0) { await RunAttackBattle(uid, sess, ct); return; }
                sess.Step = SessionStep.AttackWaitingAirStrategy;
                // FIX(1): callback صحیح attack_air_strategy (قبلاً attackair_strategy اشتباه بود)
                var kb = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "✈️ برتری هوایی",
                            "attack_air_strategy:1"
                        )
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "💣 بمباران راهبردی",
                            "attack_air_strategy:2"
                        )
                    }
                });

                await SendPrompt(
                    uid,
                    uid,
                    AirAttackStrategyGuide,
                    kb,
                    ct
                );
                return;
            }
        }

        if (txt == "وضعیت دفاع")
        {
            var chatIds = Database.GetUserChatIds(uid);
            if (chatIds.Count == 0) { await SendTemp(uid, "❌ شما در هیچ گروهی کشور ندارید.", ct: ct); return; }
            if (chatIds.Count == 1) { await SendDefenseStatus(uid, uid, chatIds[0], ct); }
            else
            {
                var allC = Database.GetAllCountries();
                var kb = chatIds.Select(cid => { var n = allC.FirstOrDefault(c => c.ChatId == cid && c.OwnerId == uid)?.Name ?? cid.ToString(); return new[] { InlineKeyboardButton.WithCallbackData(n, $"defense_status:{cid}") }; }).ToArray();
                sessions[uid] = new UserSession { Step = SessionStep.DefenseWaitingGroup };
                await SendPrompt(uid, uid, "📋 گروه:", new InlineKeyboardMarkup(kb), ct);
            }
            return;
        }

        if (txt == "ترنسفر" || txt == "انتقال" || txt == "ارسال محموله" || txt == "ارسال منابع")
        {
            var chatIds = Database.GetUserChatIds(uid);
            if (chatIds.Count == 0) { await SendTemp(uid, "❌ شما در هیچ گروهی کشور ندارید.", ct: ct); return; }
            if (chatIds.Count == 1)
            {
                long cid = chatIds[0];
                var pc = Database.GetCountry(uid, cid); if (pc != null && pc.PortLevel < 3) { await SendTemp(uid, "⚓ سطح بندر شما برای این عملیات کافی نیست! (حداقل سطح مورد نیاز: ۳)", ct: ct); return; }
                long aid = Database.GetUserAllianceId(cid, uid);
                if (aid == 0) { await SendTemp(uid, "❌ عضو هیچ اتحادی نیستید.", ct: ct); return; }
                var mems = Database.GetAllianceMembers(aid).Where(m => m != uid).ToList();
                if (mems.Count == 0) { await SendTemp(uid, "❌ اتحاد عضو دیگری ندارد.", ct: ct); return; }
                if (GetTransferCount(cid, uid) >= MAX_TRANSFERS_PER_UPDATE && !Database.HasGroupLockExemption(cid)) { await SendTemp(uid, $"⛔ سهمیه تمام شد ({MAX_TRANSFERS_PER_UPDATE}).", ct: ct); return; }
                sessions[uid] = new UserSession { Step = SessionStep.TransferWaitingResource, TransferChatId = cid, TransferAllianceId = aid };
                var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("💰 پول", $"tf_res:{cid}:money"), InlineKeyboardButton.WithCallbackData("🔩 آهن", $"tf_res:{cid}:iron") }, new[] { InlineKeyboardButton.WithCallbackData("🪖 سرباز", $"tf_res:{cid}:soldiers"), InlineKeyboardButton.WithCallbackData("🛡 تانک", $"tf_res:{cid}:tanks") }, new[] { InlineKeyboardButton.WithCallbackData("✈️ جنگنده", $"tf_res:{cid}:planes"), InlineKeyboardButton.WithCallbackData("🛩 بمب‌افکن", $"tf_res:{cid}:bombers") } });
                await SendPrompt(uid, uid, "📦 **ترنسفر**\n\nنوع منبع:", kb, ct);
            }
            else
            {
                var kb = chatIds.Select(cid => { var c = Database.GetCountry(uid, cid); return new[] { InlineKeyboardButton.WithCallbackData(c?.Name ?? $"گروه {cid}", $"tf_chat:{cid}") }; }).ToArray();
                await SendPrompt(uid, uid, "📦 گروه:", new InlineKeyboardMarkup(kb), ct);
            }
            return;
        }

        if (txt == "صف آرایی تهاجمی" || txt == "صف آرایی دفاعی" || txt == "صف‌آرایی تهاجمی" || txt == "صف‌آرایی دفاعی")
        {
            bool isOff = txt.Contains("تهاجمی");
            var chatIds = Database.GetUserChatIds(uid);
            if (chatIds.Count == 0) { await SendTemp(uid, "❌ شما کشوری ندارید.", ct: ct); return; }
            if (chatIds.Count == 1)
            {
                long cid = chatIds[0];
                var pc = Database.GetCountry(uid, cid); if (pc != null && pc.PortLevel < 3) { await SendTemp(uid, "⚓ سطح بندر شما برای این عملیات کافی نیست! (حداقل سطح مورد نیاز: ۳)", ct: ct); return; }
                long aid = Database.GetUserAllianceId(cid, uid);
                if (aid == 0) { await SendTemp(uid, "❌ عضو اتحاد نیستید.", ct: ct); return; }
                var mems = Database.GetAllianceMembers(aid);
                int dailyLimit = mems.Count <= 5 ? 1 : (mems.Count <= 10 ? 2 : (mems.Count <= 20 ? 3 : 5));
                if (Database.GetRecentAllianceDeploymentsCount(aid, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 86400000L) >= dailyLimit && !Database.HasGroupLockExemption(cid)) { await SendTemp(uid, $"⛔ سقف روزانه ({dailyLimit}) پر شد.", ct: ct); return; }
                var tgts = isOff ? Database.GetCountriesByChatId(cid).Where(c => !mems.Contains(c.OwnerId)).ToList() : mems.Select(m => Database.GetCountry(m, cid)).Where(c => c != null).ToList()!;
                if (tgts.Count == 0) { await SendTemp(uid, isOff ? "❌ هیچ هدفی نیست." : "❌ عضو معتبری برای دفاع نیست.", ct: ct); return; }
                var tkb = tgts.Select(t => new[] { InlineKeyboardButton.WithCallbackData($"🏳️ {t!.Name} ({t.OwnerName})", $"dep_target:{cid}:{aid}:{(isOff ? "Off" : "Def")}:{t.OwnerId}") }).ToArray();
                await SendPrompt(uid, uid, $"⚔️ صف‌آرایی {(isOff ? "تهاجمی" : "دفاعی")}\n🎯 کشور:", new InlineKeyboardMarkup(tkb), ct);
            }
            else
            {
                var kb = chatIds.Select(cid => { var c = Database.GetCountry(uid, cid); return new[] { InlineKeyboardButton.WithCallbackData(c?.Name ?? $"گروه {cid}", $"dep_chat:{cid}:{(isOff ? "Offensive" : "Defensive")}") }; }).ToArray();
                await SendPrompt(uid, uid, "⚔️ گروه:", new InlineKeyboardMarkup(kb), ct);
            }
            return;
        }

        if (txt == "حمله")
        {
            var chatIds = Database.GetUserChatIds(uid);
            if (chatIds.Count == 0) { await SendTemp(uid, "❌ شما کشوری ندارید.", ct: ct); return; }
            if (chatIds.Count == 1)
            {
                long cid = chatIds[0];
                var targets = Database.GetCountriesByChatId(cid).Where(c => c.OwnerId != uid).ToList();
                if (targets.Count == 0) { await SendTemp(uid, "هیچ هدفی نیست.", ct: ct); return; }
                var kb = targets.Select(t => new[] { InlineKeyboardButton.WithCallbackData(t.OwnerName, $"attack_target:{cid}:{t.OwnerId}") }).ToArray();
                sessions[uid] = new UserSession { Step = SessionStep.AttackWaitingTarget, AttackChatId = cid };
                await SendPrompt(uid, uid, "🎯 هدف:", new InlineKeyboardMarkup(kb), ct);
            }
            else
            {
                var allC = Database.GetAllCountries();
                var kb = chatIds.Select(cid => { var n = allC.FirstOrDefault(c => c.ChatId == cid && c.OwnerId == uid)?.Name ?? cid.ToString(); return new[] { InlineKeyboardButton.WithCallbackData(n, $"attack_group:{cid}") }; }).ToArray();
                sessions[uid] = new UserSession { Step = SessionStep.AttackWaitingGroup };
                await SendPrompt(uid, uid, "📋 گروه:", new InlineKeyboardMarkup(kb), ct);
            }
            return;
        }

        // FIX(3): /start و راهنما در پیوی — تا کاربر فکر نکند بات خاموش است
        if (txt == "/start" || txt == "شروع" || txt == "start")
        {
            await SendStartMessage(uid, ct);
            return;
        }
        if (txt == "راهنما" || txt == "/help" || txt == "help")
        {
            await SendPermanent(uid, HelpText, parseMode: ParseMode.Html, ct: ct);
            return;
        }

        // FIX(3): هر پیام ناشناختهٔ دیگر در پیوی → راهنمای کوتاه به‌جای سکوت
        await SendPermanent(uid,
            "ℹ️ این ربات یک بازی گروهی است و بیشتر دستورها فقط داخل گروه کار می‌کنند.\n" +
            "برای دیدن راهنمای کامل بنویسید: <b>راهنما</b>\n" +
            "برای شروع/توضیح بیشتر بنویسید: <b>/start</b>",
            parseMode: ParseMode.Html, ct: ct);
    }

    // ============================================================
    //  Callback handlers
    // ============================================================
    static async Task HandleCallbackAsync(CallbackQuery cb, CancellationToken ct)
    {
        if (cb.Data == null) return;

        if (cb.Data.StartsWith("adm:", StringComparison.Ordinal))
        {
            await HandleAdminCallbackAsync(cb, ct);
            return;
        }

        if (cb.Data.StartsWith("ally_")) { await HandleAllianceInviteCallback(cb, ct); return; }
        if (cb.Data.StartsWith("tf_")) { await HandleTransferCallback(cb, ct); return; }
        if (cb.Data.StartsWith("dep_")) { await HandleDeploymentCallback(cb, ct); return; }
        if (cb.Message == null) return;
        var parts = cb.Data.Split(':');
        if (parts.Length < 1) return;

        if (parts[0] is "eq_details" or "faction" or "build_menu" or "upgrade" or "tank_info" or "tank_buy" or "plane_info" or "plane_buy" or "bomber_info" or "bomber_buy" or "aa_info" or "aa_buy" or "cancel")
        {
            if (parts.Length >= 2 && TryParseLong(parts[1], out long ownerBtn))
            {
                if (ownerBtn != cb.From.Id) { await bot.AnswerCallbackQueryAsync(cb.Id, "⛔ این دکمه برای شما نیست!", showAlert: true, cancellationToken: ct); return; }
            }
        }

        switch (parts[0])
        {
            case "cancel": await HandleCancelCallback(cb, ct); break;
            case "faction": await HandleFactionCallback(cb, parts, ct); break;
            case "eq_details": await SendCountryEquipmentDetails(cb, parts, ct); break;
            case "build_menu": await HandleBuildMenuCallback(cb, parts, ct); break;
            case "upgrade": await HandleUpgradeCallback(cb, parts, ct); break;
            case "timing": await HandleTimingCallback(cb, parts, ct); break;
            case "tank_info": await HandleTankInfoCallback(cb, parts, ct); break;
            case "tank_buy": await HandleTankBuyCallback(cb, parts, ct); break;
            case "plane_info": await HandlePlaneInfoCallback(cb, parts, ct); break;
            case "plane_buy": await HandlePlaneBuyCallback(cb, parts, ct); break;
            case "bomber_info": await HandleBomberInfoCallback(cb, parts, ct); break;
            case "bomber_buy": await HandleBomberBuyCallback(cb, parts, ct); break;
            case "aa_info": await HandleAntiAirInfoCallback(cb, parts, ct); break;
            case "aa_buy": await HandleAntiAirBuyCallback(cb, parts, ct); break;
            case "defense_status": await HandleDefenseStatusCallback(cb, parts, ct); break;
            case "defense_tactic": await HandleDefenseTacticCallback(cb, parts, ct); break;
            case "defense_tactic_select": await HandleDefenseTacticSelectCallback(cb, parts, ct); break;
            case "defense_set": await HandleDefenseSetCallback(cb, parts, ct); break;
            case "defense_pct": await HandleDefensePctCallback(cb, parts, ct); break;
            case "airdef_strategy": await HandleAirDefStrategyCallback(cb, parts, ct); break;
            case "airdef_tactic": await HandleAirDefTacticCallback(cb, parts, ct); break;
            case "attack_group": await HandleAttackGroupCallback(cb, parts, ct); break;
            case "attack_target": await HandleAttackTargetCallback(cb, parts, ct); break;
            case "attack_strategy": await HandleAttackStrategyCallback(cb, parts, ct); break;
            case "attack_tactic": await HandleAttackTacticCallback(cb, parts, ct); break;
            case "attack_air_strategy": await HandleAttackAirStrategyCallback(cb, parts, ct); break;
            case "attack_air_tactic": await HandleAttackAirTacticCallback(cb, parts, ct); break;
        }
    }

    static async Task HandleCancelCallback(CallbackQuery cb, CancellationToken ct)
    {
        long uid = cb.From.Id;
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
        EndSession(uid);
        if (cb.Message != null) DeleteNow(cb.Message.Chat.Id, cb.Message.MessageId);
    }

    static async Task HandleFactionCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length != 3) return;
        long uid = cb.From.Id;
        string facStr = parts[2];
        Faction fac = facStr switch { "USSR" => Faction.USSR, "USA" => Faction.USA, _ => Faction.Reich };
        sessions[uid] = new UserSession { Step = SessionStep.WaitingCountryName, Faction = fac, FactionStr = facStr };
        if (cb.Message != null) { await bot.EditMessageTextAsync(cb.Message.Chat.Id, cb.Message.MessageId, "اسم کشور را وارد کنید", cancellationToken: ct); TrackPrompt(uid, cb.Message.Chat.Id, cb.Message.MessageId); }
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
    }

    static async Task HandleBuildMenuCallback(
        CallbackQuery cb,
        string[] parts,
        CancellationToken ct)
    {
        if (parts.Length != 3 || cb.Message == null)
            return;

        long uid = cb.From.Id;
        string bt = parts[2];

        if (bt is not ("factory" or "port" or "mine"))
        {
            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                "❌ ساختمان نامعتبر است.",
                showAlert: true,
                cancellationToken: ct
            );
            return;
        }

        long chatId = cb.Message.Chat.Id;
        var c = Database.GetCountry(uid, chatId);

        if (c == null)
        {
            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                "❌ کشوری ندارید!",
                cancellationToken: ct
            );
            return;
        }

        int cur = bt switch
        {
            "factory" => c.FactoryLevel,
            "port" => c.PortLevel,
            "mine" => c.MineLevel,
            _ => 1
        };

        int max = MaxBuildLevel(c, bt);

        if (cur >= max)
        {
            string maxMessage = c.Besieged >= 2
                ? "🔒 به‌دلیل شرایط بحرانی، امکان ارتقا وجود ندارد."
                : "✅ این ساختمان در حداکثر سطح است.";

            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                maxMessage,
                showAlert: true,
                cancellationToken: ct
            );
            return;
        }

        int next = cur + 1;

        double currentIncome = bt switch
        {
            "factory" => FactoryIncome[cur],
            "port" => PortIncome[cur],
            "mine" => MineIncome[cur],
            _ => 0
        };

        double nextIncome = bt switch
        {
            "factory" => FactoryIncome[next],
            "port" => PortIncome[next],
            "mine" => MineIncome[next],
            _ => 0
        };

        string buildingName = bt switch
        {
            "factory" => "🏭 کارخانه",
            "port" => "⚓ بندر",
            "mine" => "⛏️ معدن",
            _ => "ساختمان"
        };

        string incomeUnit = bt == "mine" ? "آهن" : "پول";

        bool usesRoyalCoins = bt == "mine" && next >= 6;

        string priceText;
        string balanceText;

        if (usesRoyalCoins)
        {
            int royalCost = next == 6 ? 5 : 10;
            long royalBalance = Database.GetRoyalCoins(uid);

            priceText = $"{royalCost:N0} رویال‌کوین 💎";
            balanceText = $"موجودی رویال: {royalBalance:N0}";
        }
        else
        {
            int costK = bt switch
            {
                "factory" => FactoryUpgradeCost[cur],
                "port" => PortUpgradeCost[cur],
                "mine" => MineUpgradeCost[cur],
                _ => 0
            };

            priceText = $"{costK:N0}K پول 💰";
            balanceText = $"پول: {(c.Money / 1000.0):F1}K";
        }

        string text =
            $"{buildingName}\n" +
            $"سطح فعلی: {cur}\n" +
            $"درآمد فعلی: {currentIncome:F1}K {incomeUnit}\n\n" +
            $"سطح بعدی: {next}\n" +
            $"درآمد بعدی: {nextIncome:F1}K {incomeUnit}\n" +
            $"هزینه ارتقا: {priceText}\n" +
            balanceText;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "✅ ارتقا",
                    $"upgrade:{uid}:{bt}"
                )
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "❌ لغو",
                    $"cancel:{uid}"
                )
            }
        });

        await bot.EditMessageTextAsync(
            chatId,
            cb.Message.MessageId,
            text,
            replyMarkup: keyboard,
            cancellationToken: ct
        );

        await bot.AnswerCallbackQueryAsync(
            cb.Id,
            cancellationToken: ct
        );
    }

    static async Task HandleTimingCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || cb.Message == null) return;
        long uid = cb.From.Id;
        if (parts[1] == "daily") { sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingDailyTime }; await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct); await SendPrompt(uid, cb.Message.Chat.Id, "⏰ ساعت HHMM:", ct: ct); return; }
        if (parts[1] == "minute") { sessions[uid] = new UserSession { Step = SessionStep.OwnerWaitingMinuteTime }; await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct); await SendPrompt(uid, cb.Message.Chat.Id, "⌛ هر چند دقیقه؟", ct: ct); return; }
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
    }

    static async Task HandleUpgradeCallback(
        CallbackQuery cb,
        string[] parts,
        CancellationToken ct)
    {
        if (parts.Length != 3 || cb.Message == null)
            return;

        long uid = cb.From.Id;
        string bt = parts[2];

        if (bt is not ("factory" or "port" or "mine"))
        {
            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                "❌ ساختمان نامعتبر است.",
                showAlert: true,
                cancellationToken: ct
            );
            return;
        }

        long chatId = cb.Message.Chat.Id;
        var c = Database.GetCountry(uid, chatId);

        if (c == null)
        {
            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                "❌ کشور یافت نشد!",
                cancellationToken: ct
            );
            return;
        }

        int cur = bt switch
        {
            "factory" => c.FactoryLevel,
            "port" => c.PortLevel,
            "mine" => c.MineLevel,
            _ => 1
        };

        int max = MaxBuildLevel(c, bt);

        if (cur >= max)
        {
            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                c.Besieged >= 2
                    ? "🔒 به‌دلیل شرایط بحرانی امکان ارتقا وجود ندارد."
                    : "✅ ساختمان در حداکثر سطح است.",
                showAlert: true,
                cancellationToken: ct
            );
            return;
        }

        int newLevel = cur + 1;
        bool usesRoyalCoins = bt == "mine" && newLevel >= 6;

        long moneyCost = 0;
        int royalCost = 0;

        if (usesRoyalCoins)
        {
            royalCost = newLevel == 6 ? 5 : 10;
            long royalBalance = Database.GetRoyalCoins(uid);

            if (royalBalance < royalCost)
            {
                await bot.AnswerCallbackQueryAsync(
                    cb.Id,
                    $"💎 رویال‌کوین کافی نیست!\n" +
                    $"نیاز: {royalCost:N0}\n" +
                    $"موجودی: {royalBalance:N0}",
                    showAlert: true,
                    cancellationToken: ct
                );
                return;
            }

            Database.AddRoyalCoins(uid, -royalCost);

            try
            {
                Database.UpdateBuildingLevel(
                    uid,
                    chatId,
                    bt,
                    newLevel,
                    0
                );
            }
            catch
            {
                Database.AddRoyalCoins(uid, royalCost);
                throw;
            }
        }
        else
        {
            int costK = bt switch
            {
                "factory" => FactoryUpgradeCost[cur],
                "port" => PortUpgradeCost[cur],
                "mine" => MineUpgradeCost[cur],
                _ => 0
            };

            moneyCost = costK * 1000L;

            if (c.Money < moneyCost)
            {
                await bot.AnswerCallbackQueryAsync(
                    cb.Id,
                    $"💰 پول کافی نیست!\n" +
                    $"نیاز: {costK:N0}K\n" +
                    $"موجودی: {(c.Money / 1000.0):F1}K",
                    showAlert: true,
                    cancellationToken: ct
                );
                return;
            }

            Database.UpdateBuildingLevel(
                uid,
                chatId,
                bt,
                newLevel,
                -moneyCost
            );
        }

        var updatedCountry = Database.GetCountry(uid, chatId);

        if (updatedCountry == null)
        {
            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                "⚠️ ارتقا انجام شد، اما اطلاعات جدید دریافت نشد.",
                showAlert: true,
                cancellationToken: ct
            );
            return;
        }

        bool canUpgradeMore = newLevel < max;

        string buildingName = bt switch
        {
            "factory" => "کارخانه",
            "port" => "بندر",
            "mine" => "معدن",
            _ => "ساختمان"
        };

        string currentBalance = usesRoyalCoins
            ? $"💎 رویال باقی‌مانده: {Database.GetRoyalCoins(uid):N0}"
            : $"💰 پول باقی‌مانده: {(updatedCountry.Money / 1000.0):F1}K";

        string resultText =
            $"✅ {buildingName} به سطح {newLevel} ارتقا یافت.\n" +
            currentBalance;

        InlineKeyboardMarkup? keyboard = null;

        if (canUpgradeMore)
        {
            int followingLevel = newLevel + 1;
            bool nextUsesRoyal =
                bt == "mine" && followingLevel >= 6;

            string nextPrice;

            if (nextUsesRoyal)
            {
                int nextRoyalCost =
                    followingLevel == 6 ? 5 : 10;

                nextPrice =
                    $"{nextRoyalCost:N0} رویال‌کوین";
            }
            else
            {
                int nextCostK = bt switch
                {
                    "factory" => FactoryUpgradeCost[newLevel],
                    "port" => PortUpgradeCost[newLevel],
                    "mine" => MineUpgradeCost[newLevel],
                    _ => 0
                };

                nextPrice = $"{nextCostK:N0}K پول";
            }

            resultText +=
                $"\nارتقای بعدی: سطح {followingLevel}" +
                $" — {nextPrice}";

            keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "⬆️ ارتقای بعدی",
                        $"upgrade:{uid}:{bt}"
                    )
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "❌ بستن",
                        $"cancel:{uid}"
                    )
                }
            });
        }
        else
        {
            resultText += "\n🏁 حداکثر سطح";
        }

        await bot.EditMessageTextAsync(
            chatId,
            cb.Message.MessageId,
            resultText,
            replyMarkup: keyboard,
            cancellationToken: ct
        );

        await bot.AnswerCallbackQueryAsync(
            cb.Id,
            usesRoyalCoins
                ? $"✅ {royalCost:N0} رویال‌کوین کسر شد."
                : "✅ ارتقا انجام شد.",
            cancellationToken: ct
        );
    }

    static async Task HandleTankInfoCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length != 3 || cb.Message == null) return;
        long uid = cb.From.Id;
        string tid = parts[2];
        string info = tid switch
        {
            "M2Medium" => "🇺🇸 M2 Medium\n\n⚖️ ۱۸ تن | 🔫 ۳۷mm | 🛡 ۳۰mm | ⚡ ۴۲km/h\n💰 هر ۵ تانک: ۲K آهن + ۲K پول",
            "T28" => "🇷🇺 T-28\n\n⚖️ ۲۸ تن | 🔫 ۷۶mm | 🛡 ۸۰mm | ⚡ ۳۷km/h\n💰 هر ۵ تانک: ۳K آهن + ۳K پول",
            "PanzerIII" => "🇩🇪 Panzer III\n\n⚖️ ۲۳ تن | 🔫 ۵۰mm | 🛡 ۶۰mm | ⚡ ۴۰km/h\n💰 هر ۵ تانک: ۲.۵K آهن + ۲.۵K پول",
            _ => "تانک ناشناخته"
        };
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
        if (info == "تانک ناشناخته") { await SendTemp(cb.Message.Chat.Id, info, ct: ct); return; }
        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("1", $"tank_buy:{uid}:{tid}:1"), InlineKeyboardButton.WithCallbackData("5", $"tank_buy:{uid}:{tid}:5") }, new[] { InlineKeyboardButton.WithCallbackData("10", $"tank_buy:{uid}:{tid}:10"), InlineKeyboardButton.WithCallbackData("25", $"tank_buy:{uid}:{tid}:25") }, new[] { InlineKeyboardButton.WithCallbackData("❌ انصراف", $"cancel:{uid}") } });
        await SendTemp(cb.Message.Chat.Id, info, markup: kb, ct: ct);
    }

    static async Task HandleTankBuyCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length != 4 || cb.Message == null) return;
        long uid = cb.From.Id;
        string tid = parts[2];
        if (!TryParseInt(parts[3], out int cnt) || cnt <= 0) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ تعداد نامعتبر", cancellationToken: ct); return; }
        long cid = cb.Message.Chat.Id;
        var c = Database.GetCountry(uid, cid);
        if (c == null) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ کشور یافت نشد!", cancellationToken: ct); return; }
        double i5 = tid switch { "M2Medium" => 2000, "T28" => 3000, "PanzerIII" => 2500, _ => 0 };
        double m5 = tid switch { "M2Medium" => 2000, "T28" => 3000, "PanzerIII" => 2500, _ => 0 };
        long ti = (long)Math.Ceiling(cnt / 5.0 * i5);
        long tm = (long)Math.Ceiling(cnt / 5.0 * m5);
        if (c.Iron < ti) { await bot.AnswerCallbackQueryAsync(cb.Id, $"❌ آهن: نیاز {ti / 1000.0:F1}K", cancellationToken: ct); return; }
        if (c.Money < tm) { await bot.AnswerCallbackQueryAsync(cb.Id, $"❌ پول: نیاز {tm / 1000.0:F1}K", cancellationToken: ct); return; }
        c.Iron -= ti; c.Tanks += cnt; c.Money -= tm;
        Database.UpdateCountryResources(uid, cid, c.Money, c.Iron, c.Tanks);
        string tn = tid switch { "M2Medium" => "M2 Medium", "T28" => "T-28", "PanzerIII" => "Panzer III", _ => tid };
        await SendTemp(cid, $"✅ {cnt} تانک {tn} خریداری شد!\n💰 پول: {(c.Money / 1000.0):F1}K\n🔩 آهن: {(c.Iron / 1000.0):F1}K", ct: ct);
        await bot.AnswerCallbackQueryAsync(cb.Id, "✅ خرید موفق", cancellationToken: ct);
    }

    static async Task HandlePlaneInfoCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length != 3 || cb.Message == null) return;
        long uid = cb.From.Id;
        string pid = parts[2];
        string info = pid switch
        {
            "Bf109" => "🇩🇪 Bf 109\n⚡ ۵۷۰km/h | 🎯 مانور ۸/۱۰\n💰 هر ۵: ۲K آهن + ۵K پول",
            "P36" => "🇺🇸 P-36\n⚡ ۵۰۰km/h | 🎯 مانور ۹/۱۰\n💰 هر ۵: ۱.۵K آهن + ۴K پول",
            "I16" => "🇷🇺 I-16\n⚡ ۵۲۰km/h | 🎯 مانور ۹/۱۰\n💰 هر ۵: ۱K آهن + ۳.۵K پول",
            _ => "ناشناخته"
        };
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("1", $"plane_buy:{uid}:{pid}:1"), InlineKeyboardButton.WithCallbackData("5", $"plane_buy:{uid}:{pid}:5") }, new[] { InlineKeyboardButton.WithCallbackData("10", $"plane_buy:{uid}:{pid}:10"), InlineKeyboardButton.WithCallbackData("25", $"plane_buy:{uid}:{pid}:25") }, new[] { InlineKeyboardButton.WithCallbackData("❌ انصراف", $"cancel:{uid}") } });
        await SendTemp(cb.Message.Chat.Id, info, markup: kb, ct: ct);
    }

    static async Task HandlePlaneBuyCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length != 4 || cb.Message == null) return;
        long uid = cb.From.Id;
        string pid = parts[2];
        if (!TryParseInt(parts[3], out int cnt) || cnt <= 0) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ تعداد", cancellationToken: ct); return; }
        long cid = cb.Message.Chat.Id;
        var c = Database.GetCountry(uid, cid);
        if (c == null) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ کشور", cancellationToken: ct); return; }
        double i5 = pid switch { "I16" => 1000, "P36" => 1500, "Bf109" => 2000, _ => 0 };
        double m5 = pid switch { "I16" => 3500, "P36" => 4000, "Bf109" => 5000, _ => 0 };
        long ti = (long)Math.Ceiling(cnt / 5.0 * i5);
        long tm = (long)Math.Ceiling(cnt / 5.0 * m5);
        if (c.Iron < ti) { await bot.AnswerCallbackQueryAsync(cb.Id, $"❌ آهن", cancellationToken: ct); return; }
        if (c.Money < tm) { await bot.AnswerCallbackQueryAsync(cb.Id, $"❌ پول", cancellationToken: ct); return; }
        c.Iron -= ti; c.Planes += cnt; c.Money -= tm;
        Database.UpdatePlanesResources(uid, cid, c.Money, c.Iron, c.Planes);
        string pn = pid switch { "I16" => "I-16", "P36" => "P-36", "Bf109" => "Bf 109", _ => pid };
        await SendTemp(cid, $"✅ {cnt} {pn} خریداری شد!", ct: ct);
        await bot.AnswerCallbackQueryAsync(cb.Id, "✅", cancellationToken: ct);
    }

    static async Task HandleBomberInfoCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length != 3 || cb.Message == null) return;
        long uid = cb.From.Id;
        string bid = parts[2];
        string info = bid switch
        {
            "B17" => "🇺🇸 B-17\n⚡ ۴۶۰km/h | 🛡 ۸/۱۰ | 💣 ۳۶۰۰kg\n💰 هر ۱: ۳K آهن + ۵K پول",
            "He111" => "🇩🇪 He 111\n⚡ ۴۳۵km/h | 🛡 ۵/۱۰ | 💣 ۲۰۰۰kg\n💰 هر ۱: ۲K آهن + ۴K پول",
            "DB3" => "🇷🇺 DB-3\n⚡ ۴۳۰km/h | 🛡 ۳/۱۰ | 💣 ۱۰۰۰kg\n💰 هر ۱: ۱K آهن + ۳K پول",
            _ => "ناشناخته"
        };
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("1", $"bomber_buy:{uid}:{bid}:1"), InlineKeyboardButton.WithCallbackData("2", $"bomber_buy:{uid}:{bid}:2") }, new[] { InlineKeyboardButton.WithCallbackData("5", $"bomber_buy:{uid}:{bid}:5"), InlineKeyboardButton.WithCallbackData("10", $"bomber_buy:{uid}:{bid}:10") }, new[] { InlineKeyboardButton.WithCallbackData("❌ انصراف", $"cancel:{uid}") } });
        await SendTemp(cb.Message.Chat.Id, info, markup: kb, ct: ct);
    }

    static async Task HandleBomberBuyCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length != 4 || cb.Message == null) return;
        long uid = cb.From.Id;
        string bid = parts[2];
        if (!TryParseInt(parts[3], out int cnt) || cnt <= 0) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ تعداد", cancellationToken: ct); return; }
        long cid = cb.Message.Chat.Id;
        var c = Database.GetCountry(uid, cid);
        if (c == null) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ کشور", cancellationToken: ct); return; }
        double i1 = bid switch { "DB3" => 1000, "He111" => 2000, "B17" => 3000, _ => 0 };
        double m1 = bid switch { "DB3" => 3000, "He111" => 4000, "B17" => 5000, _ => 0 };
        long ti = (long)(cnt * i1);
        long tm = (long)(cnt * m1);
        if (c.Iron < ti) { await bot.AnswerCallbackQueryAsync(cb.Id, $"❌ آهن", cancellationToken: ct); return; }
        if (c.Money < tm) { await bot.AnswerCallbackQueryAsync(cb.Id, $"❌ پول", cancellationToken: ct); return; }
        c.Iron -= ti; c.Bombers += cnt; c.Money -= tm;
        Database.UpdateBombersResources(uid, cid, c.Money, c.Iron, c.Bombers);
        string bn = bid switch { "DB3" => "DB-3", "He111" => "He 111", "B17" => "B-17", _ => bid };
        await SendTemp(cid, $"✅ {cnt} {bn} خریداری شد!", ct: ct);
        await bot.AnswerCallbackQueryAsync(cb.Id, "✅", cancellationToken: ct);
    }

    static async Task HandleAntiAirInfoCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length != 3 || cb.Message == null) return;
        long uid = cb.From.Id;
        string info = "🎯 توپ ۷۶mm\n💰 هر ۵: ۲K آهن + ۴K پول";
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
        // FIX(1): همهٔ callbackها aa_buy (قبلاً یکی اشتباه aabuy بود)
        var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("1", $"aa_buy:{uid}:AA76:1"), InlineKeyboardButton.WithCallbackData("5", $"aa_buy:{uid}:AA76:5") }, new[] { InlineKeyboardButton.WithCallbackData("10", $"aa_buy:{uid}:AA76:10"), InlineKeyboardButton.WithCallbackData("25", $"aa_buy:{uid}:AA76:25") }, new[] { InlineKeyboardButton.WithCallbackData("❌ انصراف", $"cancel:{uid}") } });
        await SendTemp(cb.Message.Chat.Id, info, markup: kb, ct: ct);
    }

    static async Task HandleAntiAirBuyCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length != 4 || cb.Message == null) return;
        long uid = cb.From.Id;
        if (!TryParseInt(parts[3], out int cnt) || cnt <= 0) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ تعداد", cancellationToken: ct); return; }
        long cid = cb.Message.Chat.Id;
        var c = Database.GetCountry(uid, cid);
        if (c == null) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ کشور", cancellationToken: ct); return; }
        long ti = (long)Math.Ceiling(cnt / 5.0 * 2000);
        long tm = (long)Math.Ceiling(cnt / 5.0 * 4000);
        if (c.Iron < ti) { await bot.AnswerCallbackQueryAsync(cb.Id, $"❌ آهن", cancellationToken: ct); return; }
        if (c.Money < tm) { await bot.AnswerCallbackQueryAsync(cb.Id, $"❌ پول", cancellationToken: ct); return; }
        c.Iron -= ti; c.AntiAir += cnt; c.Money -= tm;
        Database.UpdateAntiAirResources(uid, cid, c.Money, c.Iron, c.AntiAir);
        await SendTemp(cid, $"✅ {cnt} پدافند خریداری شد!", ct: ct);
        await bot.AnswerCallbackQueryAsync(cb.Id, "✅", cancellationToken: ct);
    }

    // ============================================================
    //  تایمر آپدیت دارایی — نسخه اصلاح‌شده (FIXED)
    // ============================================================
    static void StartAssetUpdateTimer()
    {
        try
        {
            assetUpdateTimer?.Dispose();
            assetUpdateTimer = null;
            if (UpdateMode == "minute")
            {
                long msLong = (long)UpdateValue * 60L * 1000L;
                if (msLong < 1000) msLong = 1000;
                var due = TimeSpan.FromMilliseconds(msLong);
                assetUpdateTimer = new Timer(async _ =>
                {
                    try { await RunAssetUpdate(); }
                    catch (Exception ex) { Console.WriteLine($"[TIMER RUN ERR] {ex.Message}"); }
                }, null, due, due);
                Console.WriteLine($"[TIMER] minute mode: every {UpdateValue} min");
            }
            else
            {
                var now = GetTehranNow();
                var target = new DateTime(now.Year, now.Month, now.Day, UpdateValue / 60, UpdateValue % 60, 0);
                if (target <= now) target = target.AddDays(1);
                TimeSpan delay = target - now;
                if (delay < TimeSpan.Zero) delay = TimeSpan.FromMinutes(1);
                if (delay > TimeSpan.FromDays(2)) delay = TimeSpan.FromDays(2);
                assetUpdateTimer = new Timer(async _ =>
                {
                    try { await RunAssetUpdate(); }
                    catch (Exception ex) { Console.WriteLine($"[TIMER RUN ERR] {ex.Message}"); }
                    try { StartAssetUpdateTimer(); }
                    catch (Exception ex) { Console.WriteLine($"[TIMER RESCHEDULE ERR] {ex.Message}"); }
                }, null, delay, Timeout.InfiniteTimeSpan);
                Console.WriteLine($"[TIMER] daily mode: next run in {delay.TotalMinutes:F1} min (Tehran target {target:yyyy-MM-dd HH:mm})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TIMER SETUP ERR] {ex.Message} — retry in 60s");
            try
            {
                assetUpdateTimer?.Dispose();
                assetUpdateTimer = new Timer(_ =>
                {
                    try { StartAssetUpdateTimer(); } catch { }
                }, null, TimeSpan.FromMinutes(1), Timeout.InfiniteTimeSpan);
            }
            catch { }
        }
    }

        static void StartTransferTimer()
    {
        try
        {
            transferTimer?.Dispose();
            transferTimer = null;
            transferTimer = new Timer(async _ =>
            {
                try { await ProcessActiveTransfers(CancellationToken.None); }
                catch (Exception ex) { Console.WriteLine($"[TRANSFER TIMER ERR] {ex.Message}"); }
                try { await ProcessActiveDeployments(CancellationToken.None); }
                catch (Exception ex) { Console.WriteLine($"[DEPLOY TIMER ERR] {ex.Message}"); }
            }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            Console.WriteLine("[TIMER] transfer/deployment timer started (every 60s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRANSFER TIMER SETUP ERR] {ex.Message}");
        }
    }

    static double GetPopulationFactor(long population) => 1.0;
    static int MaxBuildLevel(Country c, string buildingType) => c.Besieged >= 2 ? 3 : buildingType == "mine" ? 7 : 5;
    static double SiegeIncomeFactor(Country c) => c.Besieged >= 2 ? 0.5 : 1.0;

    static long CalcTaxIncome(Country c)
    {
        double income = c.Population * (c.TaxRate / 100.0) * 0.3;
        return (long)Math.Max(0, income);
    }

    static double WelfareTarget(Country c)
    {
        double portBoost = c.PortLevel > 0 ? 5.0 : 0.0;
        double target = 100.0 - c.TaxRate - (c.RecruitmentRate * 3.0) + portBoost;
        return Math.Clamp(target, 0.0, 100.0);
    }

    static double NextWelfare(Country c)
    {
        double target = WelfareTarget(c);
        double next = c.Welfare + (target - c.Welfare) * 0.5;
        return Math.Clamp(next, 0.0, 100.0);
    }

    static long CalcBuildingMoney(Country c)
    {
        return (long)((FactoryIncome[c.FactoryLevel] + PortIncome[c.PortLevel]) * 1000);
    }

    static long CalcIronIncome(Country c)
    {
        return (long)(MineIncome[c.MineLevel] * 1000);
    }

    static long CalcManpower(Country c)
    {
        double popPower = (c.Population / 1000.0) * (c.Welfare / 100.0);
        double nonTaxIncome = CalcBuildingMoney(c) + CalcIronIncome(c);
        double incomePower = nonTaxIncome / 20.0;
        double groundPower = (c.Soldiers / 20.0) + (c.Tanks * 15);
        double airPower = (c.Planes * 12) + (c.Bombers * 25);
        double otherPower = (c.Cities * 50) + (c.AntiAir * 8) + (c.RecruitmentRate * 40) + (c.DefenseWins * 30);
        return (long)Math.Ceiling(Math.Max(0, popPower + incomePower + groundPower + airPower + otherPower));
    }

    static bool IsSuperpowerCollision(long chatId, long leaderId, long targetId, out string reason)
    {
        reason = "";
        var all = Database.GetCountriesByChatId(chatId).OrderByDescending(c => CalcManpower(c)).ToList();
        if (all.Count <= 1) return false;
        var leader = all.FirstOrDefault(c => c.OwnerId == leaderId);
        var target = all.FirstOrDefault(c => c.OwnerId == targetId);
        if (leader == null || target == null) return false;
        int leaderRank = all.IndexOf(leader) + 1;
        int targetRank = all.IndexOf(target) + 1;
        double totalMp = all.Sum(c => CalcManpower(c));
        double leaderMp = CalcManpower(leader);
        double targetMp = CalcManpower(target);
        if (all.Count >= 3 && leaderRank <= 2 && targetRank <= 2) { reason = "رتبه ۱ و ۲ نمی‌توانند هم‌اتحاد شوند."; return true; }
        if (all.Count >= 4 && leaderRank <= 3 && targetRank <= 3 && (leaderMp + targetMp) > (totalMp * 0.40)) { reason = "ترکیب دو قدرت برتر باعث ابرقدرت می‌شود."; return true; }
        long aid = Database.GetUserAllianceId(chatId, leaderId);
        double curAllianceMp = leaderMp;
        if (aid > 0) { var members = Database.GetAllianceMembers(aid); curAllianceMp = members.Sum(m => { var c = all.FirstOrDefault(x => x.OwnerId == m); return c != null ? CalcManpower(c) : 0; }); }
        double avgMp = totalMp / all.Count;
        if (targetMp > (avgMp * 1.3) && (curAllianceMp + targetMp) > (totalMp * 0.45) && all.Count >= 3) { reason = "مان‌پاور اتحاد از حد مجاز فراتر می‌رود."; return true; }
        return false;
    }

    static async Task HandleAllianceInviteCallback(CallbackQuery cb, CancellationToken ct)
    {
        if (cb.Data == null || cb.From == null) return;
        var parts = cb.Data.Split(':');
        if (parts.Length < 2 || !TryParseLong(parts[1], out long invId)) return;
        var inv = Database.GetAllianceInvite(invId);
        if (inv == null) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ منقضی شده.", showAlert: true, cancellationToken: ct); if (cb.Message != null) DeleteNow(cb.Message.Chat.Id, cb.Message.MessageId); return; }
        if (cb.From.Id != inv.TargetUserId) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ برای شما نیست!", showAlert: true, cancellationToken: ct); return; }
        string action = parts[0];
        if (action == "ally_reject") { Database.DeleteAllianceInvite(invId); await bot.AnswerCallbackQueryAsync(cb.Id, "❌ رد شد.", cancellationToken: ct); if (cb.Message != null) await bot.EditMessageTextAsync(cb.Message.Chat.Id, cb.Message.MessageId, "❌ رد شد.", cancellationToken: ct); try { await bot.SendTextMessageAsync(inv.LeaderId, "❌ دعوت رد شد.", cancellationToken: ct); } catch { } return; }
        if (action == "ally_accept")
        {
            var alliance = Database.GetAllianceById(inv.AllianceId);
            if (alliance == null) { Database.DeleteAllianceInvite(invId); await bot.AnswerCallbackQueryAsync(cb.Id, "❌ اتحاد منحل شده.", showAlert: true, cancellationToken: ct); return; }
            if (Database.GetUserAllianceId(inv.ChatId, inv.TargetUserId) > 0) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ عضو اتحاد دیگری هستید!", showAlert: true, cancellationToken: ct); return; }
            int totalPlayers = Database.GetCountriesByChatId(inv.ChatId).Count;
            int maxMembers = Math.Max(2, totalPlayers / 2);
            if (Database.GetAllianceMembers(inv.AllianceId).Count >= maxMembers) { await bot.AnswerCallbackQueryAsync(cb.Id, "⛔ ظرفیت پر!", showAlert: true, cancellationToken: ct); return; }
            if (IsSuperpowerCollision(inv.ChatId, inv.LeaderId, inv.TargetUserId, out string reason)) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ " + reason, showAlert: true, cancellationToken: ct); return; }
            Database.AddAllianceMember(inv.AllianceId, inv.ChatId, inv.TargetUserId);
            Database.DeleteUserInvites(inv.ChatId, inv.TargetUserId);
            await bot.AnswerCallbackQueryAsync(cb.Id, "🎉 عضو شدید!", cancellationToken: ct);
            if (cb.Message != null) await bot.EditMessageTextAsync(cb.Message.Chat.Id, cb.Message.MessageId, $"🎉 به اتحاد «{alliance.Name}» پیوستید!", cancellationToken: ct);
            var tc = Database.GetCountry(inv.TargetUserId, inv.ChatId);
            try { await SendPermanent(inv.ChatId, $"🎉 کشور {tc?.Name} ({tc?.OwnerName}) به اتحاد «{alliance.Name}» پیوست! 🤝", ct: ct); } catch { }
        }
    }

    static async Task HandleTransferCallback(CallbackQuery cb, CancellationToken ct)
    {
        if (cb.Data == null || cb.From == null) return;
        long uid = cb.From.Id;
        var parts = cb.Data.Split(':');
        if (parts.Length < 2) return;
        string action = parts[0];

        if (action == "tf_chat")
        {
            if (!TryParseLong(parts[1], out long cid)) return;
            long aid = Database.GetUserAllianceId(cid, uid);
            if (aid == 0) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ عضو اتحاد نیستید.", showAlert: true, cancellationToken: ct); return; }
            var mems = Database.GetAllianceMembers(aid).Where(m => m != uid).ToList();
            if (mems.Count == 0) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ عضو دیگری نیست.", showAlert: true, cancellationToken: ct); return; }
            if (GetTransferCount(cid, uid) >= MAX_TRANSFERS_PER_UPDATE && !Database.HasGroupLockExemption(cid)) { await bot.AnswerCallbackQueryAsync(cb.Id, $"⛔ سهمیه تمام شد.", showAlert: true, cancellationToken: ct); return; }
            sessions[uid] = new UserSession { Step = SessionStep.TransferWaitingResource, TransferChatId = cid, TransferAllianceId = aid };
            await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            var kb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("💰 پول", $"tf_res:{cid}:money"), InlineKeyboardButton.WithCallbackData("🔩 آهن", $"tf_res:{cid}:iron") }, new[] { InlineKeyboardButton.WithCallbackData("🪖 سرباز", $"tf_res:{cid}:soldiers"), InlineKeyboardButton.WithCallbackData("🛡 تانک", $"tf_res:{cid}:tanks") }, new[] { InlineKeyboardButton.WithCallbackData("✈️ جنگنده", $"tf_res:{cid}:planes"), InlineKeyboardButton.WithCallbackData("🛩 بمب‌افکن", $"tf_res:{cid}:bombers") } });
            if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, "📦 نوع منبع:", replyMarkup: kb, cancellationToken: ct);
            return;
        }

        if (action == "tf_res")
        {
            if (parts.Length < 3 || !TryParseLong(parts[1], out long cid)) return;
            string res = parts[2];
            long aid = Database.GetUserAllianceId(cid, uid);
            if (aid == 0) return;
            var mems = Database.GetAllianceMembers(aid).Where(m => m != uid).ToList();
            var kbList = mems.Select(m => { var c = Database.GetCountry(m, cid); return new[] { InlineKeyboardButton.WithCallbackData($"👑 {(c?.OwnerName ?? $"کاربر {m}")} ({c?.Name})", $"tf_target:{cid}:{res}:{m}") }; }).ToArray();
            sessions[uid] = new UserSession { Step = SessionStep.TransferWaitingTarget, TransferChatId = cid, TransferAllianceId = aid, TransferResourceType = res };
            await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, "🎯 مقصد:", replyMarkup: new InlineKeyboardMarkup(kbList), cancellationToken: ct);
            return;
        }

        if (action == "tf_target")
        {
            if (parts.Length < 4 || !TryParseLong(parts[1], out long cid) || !TryParseLong(parts[3], out long tgtId)) return;
            string res = parts[2];
            long aid = Database.GetUserAllianceId(cid, uid);
            if (aid == 0) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ عضو اتحاد نیستید.", showAlert: true, cancellationToken: ct); return; }
            if (Database.GetUserAllianceId(cid, tgtId) != aid) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ هم‌اتحاد نیست.", showAlert: true, cancellationToken: ct); return; }
            var sess = sessions.GetOrAdd(uid, _ => new UserSession());
            sess.Step = SessionStep.TransferWaitingDuration; sess.TransferChatId = cid; sess.TransferAllianceId = aid; sess.TransferResourceType = res; sess.TransferTargetId = tgtId;
            await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            var durKb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⚡ ۱۵ دقیقه", $"tf_dur:15"), InlineKeyboardButton.WithCallbackData("🚀 ۳۰ دقیقه", $"tf_dur:30") }, new[] { InlineKeyboardButton.WithCallbackData("🚚 ۱ ساعت", $"tf_dur:60"), InlineKeyboardButton.WithCallbackData("🐢 ۲ ساعت", $"tf_dur:120") } });
            if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, "⏳ زمان:", replyMarkup: durKb, cancellationToken: ct);
            return;
        }

        if (action == "tf_dur")
        {
            if (parts.Length < 2 || !TryParseInt(parts[1], out int min)) return;
            if (!sessions.TryGetValue(uid, out var sess) || sess == null) return;
            sess.TransferDurationMin = min;
            sess.Step = SessionStep.TransferWaitingAmount;
            var c = Database.GetCountry(uid, sess.TransferChatId);
            long avail = 0;
            if (c != null) avail = sess.TransferResourceType switch { "money" => c.Money, "iron" => c.Iron, "soldiers" => c.Soldiers, "tanks" => c.Tanks, "planes" => c.Planes, _ => c.Bombers };
            string rn = GetResName(sess.TransferResourceType);
            await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, $"🔢 مقدار:\n📦 {rn}\n📊 موجودی: {avail:N0}", cancellationToken: ct);
            return;
        }
    }

    static async Task HandleDeploymentCallback(CallbackQuery cb, CancellationToken ct)
    {
        if (cb.Data == null || cb.From == null) return;
        long uid = cb.From.Id;
        var parts = cb.Data.Split(':');
        if (parts.Length < 2) return;
        string action = parts[0];

        if (action == "dep_chat")
        {
            if (parts.Length < 3 || !TryParseLong(parts[1], out long cid)) return;
            bool isOff = parts[2] == "Offensive";
            long aid = Database.GetUserAllianceId(cid, uid);
            if (aid == 0) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ عضو اتحاد نیستید.", showAlert: true, cancellationToken: ct); return; }
            var mems = Database.GetAllianceMembers(aid);
            var tgts = isOff ? Database.GetCountriesByChatId(cid).Where(c => !mems.Contains(c.OwnerId)).ToList() : mems.Select(m => Database.GetCountry(m, cid)).Where(c => c != null).ToList()!;
            if (tgts.Count == 0) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ هدفی نیست.", showAlert: true, cancellationToken: ct); return; }
            var tkb = tgts.Select(t => new[] { InlineKeyboardButton.WithCallbackData($"🏳️ {t!.Name} ({t.OwnerName})", $"dep_target:{cid}:{aid}:{(isOff ? "Off" : "Def")}:{t.OwnerId}") }).ToArray();
            await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, $"⚔️ صف‌آرایی {(isOff ? "تهاجمی" : "دفاعی")}\n🎯 کشور:", replyMarkup: new InlineKeyboardMarkup(tkb), cancellationToken: ct);
            return;
        }

        if (action == "dep_target")
        {
            if (parts.Length < 5) return;
            if (!TryParseLong(parts[1], out long cid) || !TryParseLong(parts[2], out long aid) || !TryParseLong(parts[4], out long tid)) return;
            string typeStr = parts[3] == "Off" ? "Offensive" : "Defensive";
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (typeStr == "Offensive" && Database.HasRecentTargetDeployment(cid, tid, nowMs - 86400000L) && !Database.HasGroupLockExemption(cid))
            { await bot.AnswerCallbackQueryAsync(cb.Id, "⛔ ۲۴ ساعت گذشته صف‌آرایی علیه این هدف اعلام شده!", showAlert: true, cancellationToken: ct); return; }
            sessions[uid] = new UserSession { Step = SessionStep.DeployWaitingDuration, DeployChatId = cid, DeployAllianceId = aid, DeployType = typeStr, DeployTargetId = tid };
            await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            var durKb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⏳ ۱ ساعت", $"dep_dur:1"), InlineKeyboardButton.WithCallbackData("⏳ ۲ ساعت", $"dep_dur:2") }, new[] { InlineKeyboardButton.WithCallbackData("⏳ ۳ ساعت", $"dep_dur:3") } });
            if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, "⏳ مدت:", replyMarkup: durKb, cancellationToken: ct);
            return;
        }

        if (action == "dep_dur")
        {
            if (parts.Length < 2 || !TryParseInt(parts[1], out int dur)) return;
            if (!sessions.TryGetValue(uid, out var sess) || sess == null) return;
            sess.DeployDuration = dur;
            sess.Step = SessionStep.DeployWaitingFormation;
            await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            var formKb = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🤝 یکپارچه", $"dep_form:Unified") }, new[] { InlineKeyboardButton.WithCallbackData("🔀 چند جبهه‌ای", $"dep_form:MultiFront") } });
            if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, "🧩 نوع آرایش:", replyMarkup: formKb, cancellationToken: ct);
            return;
        }

        if (action == "dep_form")
        {
            if (parts.Length < 2) return;
            if (!sessions.TryGetValue(uid, out var sess) || sess == null) return;
            sess.DeployFormation = parts[1];
            if (sess.DeployFormation == "Unified")
            {
                sess.Step = SessionStep.DeployWaitingStrategy;
                await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
                var sk = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⚔️ هجوم سریع", $"dep_strat:1") }, new[] { InlineKeyboardButton.WithCallbackData("🛡 ضدحمله", $"dep_strat:2") } });
                if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, "🎯 استراتژی:", replyMarkup: sk, cancellationToken: ct);
                return;
            }
            else
            {
                sess.Step = SessionStep.DeployWaitingTanks;
                await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
                var c = Database.GetCountry(uid, sess.DeployChatId);
                if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, $"🛡 تانک:\nموجود: {c?.Tanks ?? 0}", cancellationToken: ct);
                return;
            }
        }

        if (action == "dep_strat")
        {
            if (parts.Length < 2 || !TryParseInt(parts[1], out int str)) return;
            if (!sessions.TryGetValue(uid, out var sess) || sess == null) return;
            sess.DeployStrategy = str;
            sess.Step = SessionStep.DeployWaitingTactic;
            await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            var tk = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔥 ضربتی", $"dep_tac:1") }, new[] { InlineKeyboardButton.WithCallbackData("🎯 محاصره‌ای", $"dep_tac:2") } });
            if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, "🎯 تاکتیک:", replyMarkup: tk, cancellationToken: ct);
            return;
        }

        if (action == "dep_tac")
        {
            if (parts.Length < 2 || !TryParseInt(parts[1], out int tac)) return;
            if (!sessions.TryGetValue(uid, out var sess) || sess == null) return;
            sess.DeployTactic = tac;
            sess.Step = SessionStep.DeployWaitingTanks;
            await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            var c = Database.GetCountry(uid, sess.DeployChatId);
            if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, $"🛡 تانک:\nموجود: {c?.Tanks ?? 0}", cancellationToken: ct);
            return;
        }

        if (action == "dep_join")
        {
            if (parts.Length < 2 || !TryParseLong(parts[1], out long depId)) return;
            var dep = Database.GetDeploymentById(depId);
            if (dep == null) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ پایان یافته.", showAlert: true, cancellationToken: ct); return; }
            var depC = Database.GetCountry(uid, dep.ChatId); if (depC != null && depC.PortLevel < 3) { await bot.AnswerCallbackQueryAsync(cb.Id, "⚓ سطح بندر شما برای اعزام نیرو کافی نیست! (حداقل سطح: ۳)", showAlert: true, cancellationToken: ct); return; }
            long aid = Database.GetUserAllianceId(dep.ChatId, uid);
            if (aid != dep.AllianceId) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ اتحاد شما نیست!", showAlert: true, cancellationToken: ct); return; }
            if (dep.EndAtMs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ مهلت تمام شد.", showAlert: true, cancellationToken: ct); return; }
            sessions[uid] = new UserSession { DeployJoinId = dep.Id, DeployChatId = dep.ChatId, DeployAllianceId = dep.AllianceId };
            await bot.AnswerCallbackQueryAsync(cb.Id, "⚔️ به پی‌وی هدایت شدید.", cancellationToken: ct);
            if (dep.FormationType == "MultiFront")
            {
                sessions[uid].Step = SessionStep.DeployJoinWaitingStrategy;
                var sk = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⚔️ هجوم سریع", $"dep_jstrat:{dep.Id}:1") }, new[] { InlineKeyboardButton.WithCallbackData("🛡 ضدحمله", $"dep_jstrat:{dep.Id}:2") } });
                try { await bot.SendTextMessageAsync(uid, "🧩 استراتژی یگان کمکی:", replyMarkup: sk, cancellationToken: ct); }
                catch { await bot.AnswerCallbackQueryAsync(cb.Id, "⚠️ ابتدا ربات را در پیوی استارت کنید.", showAlert: true, cancellationToken: ct); }
            }
            else
            {
                sessions[uid].Step = SessionStep.DeployJoinWaitingTanks;
                var c = Database.GetCountry(uid, dep.ChatId);
                try { await bot.SendTextMessageAsync(uid, $"🤝 مشارکت یکپارچه\n🛡 تانک:\nموجود: {c?.Tanks ?? 0}", cancellationToken: ct); }
                catch { await bot.AnswerCallbackQueryAsync(cb.Id, "⚠️ ابتدا ربات را در پیوی استارت کنید.", showAlert: true, cancellationToken: ct); }
            }
            return;
        }

        if (action == "dep_jstrat")
        {
            if (parts.Length < 3 || !TryParseLong(parts[1], out long depId) || !TryParseInt(parts[2], out int str)) return;
            if (!sessions.TryGetValue(uid, out var sess) || sess == null) return;
            sess.DeployJoinStrategy = str;
            sess.Step = SessionStep.DeployJoinWaitingTactic;
            await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            var tk = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔥 ضربتی", $"dep_jtac:{depId}:1") }, new[] { InlineKeyboardButton.WithCallbackData("🎯 محاصره‌ای", $"dep_jtac:{depId}:2") } });
            if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, "🎯 تاکتیک:", replyMarkup: tk, cancellationToken: ct);
            return;
        }

        if (action == "dep_jtac")
        {
            if (parts.Length < 3 || !TryParseLong(parts[1], out long depId) || !TryParseInt(parts[2], out int tac)) return;
            if (!sessions.TryGetValue(uid, out var sess) || sess == null) return;
            sess.DeployJoinTactic = tac;
            sess.Step = SessionStep.DeployJoinWaitingTanks;
            await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
            var c = Database.GetCountry(uid, sess.DeployChatId);
            if (cb.Message != null) await bot.EditMessageTextAsync(uid, cb.Message.MessageId, $"🛡 تانک:\nموجود: {c?.Tanks ?? 0}", cancellationToken: ct);
            return;
        }

        if (action == "dep_cancel")
        {
            if (parts.Length < 2 || !TryParseLong(parts[1], out long depId)) return;
            var dep = Database.GetDeploymentById(depId);
            if (dep == null) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ قبلاً خاتمه یافته.", showAlert: true, cancellationToken: ct); return; }
            var alliance = Database.GetAllianceById(dep.AllianceId);
            if (alliance == null || (dep.InitiatorId != uid && alliance.LeaderId != uid)) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ دسترسی ندارید.", showAlert: true, cancellationToken: ct); return; }
            // FIX(2): آنپین و حذف پیام صف‌آرایی هنگام لغو از طریق دکمه
            await UnpinAndDeleteAnnounce(dep.ChatId, dep.AnnounceMsgId, ct);
            Database.CancelDeploymentForces(dep);
            await bot.AnswerCallbackQueryAsync(cb.Id, "✅ لغو شد.", cancellationToken: ct);
            if (cb.Message != null) DeleteNow(cb.Message.Chat.Id, cb.Message.MessageId);
            try { await SendPermanent(dep.ChatId, "🚫 صف‌آرایی لغو شد.", ct: ct); } catch { }
            return;
        }
    }

    static async Task SendCountryInfo(long chatId, Country c, CancellationToken ct)
    {
        double bInc = CalcBuildingMoney(c);
        double tInc = CalcTaxIncome(c);
        double iInc = CalcIronIncome(c) * SiegeIncomeFactor(c);
        bInc *= SiegeIncomeFactor(c);
        tInc *= SiegeIncomeFactor(c);
        double birthRate = c.Welfare / 100.0 * 0.05;
        double wTarget = WelfareTarget(c);
        string status = c.Besieged switch { 2 => "🆘 بحرانی", 1 => "⚠️ تحت محاصره", _ => "🏛 باثبات" };
        long mp = CalcManpower(c);
        string crisis = c.Besieged >= 2 ? "🆘 بحرانی! (۵۰٪ درآمد، قفل سطح ۴-۵)\n\n" : "";
        string info = crisis + $"🏳️ کشور: {c.Name}\n👤 مالک: {c.OwnerName}\n{status}\n⚡ مان‌پاور: {mp / 1000.0:F1}K\n\n" +
            $"💰 پول: {(c.Money / 1000.0):F1}K\n🏭 ساختمان: +{bInc / 1000.0:F1}K\n🧾 مالیات: +{tInc / 1000.0:F1}K ({c.TaxRate}%)\n\n" +
            $"🔩 آهن: {(c.Iron / 1000.0):F1}K\n⛏️ معدن: +{iInc / 1000.0:F1}K\n\n" +
            $"👥 جمعیت: {(c.Population / 1000.0):F1}K\n📊 تولد: {birthRate * 100:F2}%\n🏙 شهرها: {c.Cities}\n\n" +
            $"🪖 سرباز: {(c.Soldiers / 1000.0):F1}K\n🎯 سربازگیری: {c.RecruitmentRate}\n🏥 رفاه: {c.Welfare:F1}% (هدف: {wTarget:F0}%)\n\n" +
            $"🪖 تانک: {c.Tanks}\n✈️ جنگنده: {c.Planes}\n🛩 بمب‌افکن: {c.Bombers}\n🎯 پدافند: {c.AntiAir}\n\n" +
            $"🏭 کارخانه: {c.FactoryLevel} | ⚓ بندر: {c.PortLevel} | ⛏️ معدن: {c.MineLevel}";
        var kbDetails = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⚔️ جزئیات نظامی", $"eq_details:{c.OwnerId}") } });
        if (!string.IsNullOrEmpty(c.FlagFileId)) await SendTempPhoto(chatId, c.FlagFileId, info, markup: kbDetails, ct: ct);
        else await SendTemp(chatId, info, markup: kbDetails, ct: ct);
    }

    static async Task SendCountryEquipmentDetails(CallbackQuery cb, string[] parts, CancellationToken ct) { if (parts.Length < 2 || cb.Message == null) return; if (!TryParseLong(parts[1], out long targetUid)) return; long chatId = cb.Message.Chat.Id; var c = Database.GetCountry(targetUid, chatId); if (c == null) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ کشور یافت نشد.", showAlert: true, cancellationToken: ct); return; } var fTanks = Database.GetEquipmentModels(targetUid, chatId, "Tanks"); long sumFTanks = fTanks.Sum(x => x.Count); long domTanks = Math.Max(0, c.Tanks - sumFTanks); var tList = new List<string>(); if (domTanks > 0) tList.Add($"  • {Database.GetDefaultTankModel(c.Faction)}: {domTanks:N0} عدد"); foreach (var ft in fTanks) tList.Add($"  • {ft.ModelName}: {ft.Count:N0} عدد"); if (tList.Count == 0) tList.Add("  • هیچ تانکی موجود نمی‌باشد."); var fPlanes = Database.GetEquipmentModels(targetUid, chatId, "Planes"); long sumFPlanes = fPlanes.Sum(x => x.Count); long domPlanes = Math.Max(0, c.Planes - sumFPlanes); var pList = new List<string>(); if (domPlanes > 0) pList.Add($"  • {Database.GetDefaultPlaneModel(c.Faction)}: {domPlanes:N0} عدد"); foreach (var fp in fPlanes) pList.Add($"  • {fp.ModelName}: {fp.Count:N0} عدد"); if (pList.Count == 0) pList.Add("  • هیچ جنگنده‌ای موجود نمی‌باشد."); var fBombers = Database.GetEquipmentModels(targetUid, chatId, "Bombers"); long sumFBombers = fBombers.Sum(x => x.Count); long domBombers = Math.Max(0, c.Bombers - sumFBombers); var bList = new List<string>(); if (domBombers > 0) bList.Add($"  • {Database.GetDefaultBomberModel(c.Faction)}: {domBombers:N0} عدد"); foreach (var fb in fBombers) bList.Add($"  • {fb.ModelName}: {fb.Count:N0} عدد"); if (bList.Count == 0) bList.Add("  • هیچ بمب‌افکنی موجود نمی‌باشد."); string msg = $"⚔️ <b>جزئیات و تفکیک تجهیزات نظامی {c.Name}:</b>\n\n🛡 <b>تجهیزات زرهی (تانک‌ها):</b>\n{string.Join("\n", tList)}\n\n✈️ <b>نیروی هوایی (جنگنده‌ها):</b>\n{string.Join("\n", pList)}🛩 <b>بمب‌افکن‌های راهبردی:</b>\n{string.Join("\n", bList)}\n\n🎯 <b>پدافند هوایی:</b> {c.AntiAir:N0} عدد"; await bot.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Html, replyToMessageId: cb.Message.MessageId, cancellationToken: ct); await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct); }
    static string FullName(User u) => $"{u.FirstName} {u.LastName}".Trim();

                    static string FormatRemaining(long ms)
    {
        var t = TimeSpan.FromMilliseconds(ms);
        int h = (int)t.TotalHours;
        int m = t.Minutes;
        if (h > 0 && m > 0) return $"{h} ساعت و {m} دقیقه";
        if (h > 0) return $"{h} ساعت";
        if (m > 0) return $"{m} دقیقه";
        return "کمتر از یک دقیقه";
    }

    static string FormatTime(long unixMs)
    {
        try { return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).ToOffset(TehranOffset).ToString("HH:mm"); }
        catch { return "نامشخص"; }
    }

    static string HtmlTag(string? name, long uid)
    {
        string clean = (string.IsNullOrEmpty(name) ? $"کاربر {uid}" : name).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        return $"<a href=\"tg://user?id={uid}\">{clean}</a>";
    }

    static async Task RunAssetUpdate()
    {
        if (Interlocked.Exchange(ref assetUpdateRunning, 1) == 1)
        {
            Console.WriteLine("[TIMER] skipped: previous run still in progress");
            return;
        }
        try
        {
            if ((DateTime.UtcNow - lastAssetRunUtc).TotalSeconds < 30)
            {
                Console.WriteLine("[TIMER] skipped: ran too recently");
                return;
            }
            lastAssetRunUtc = DateTime.UtcNow;
            await RunAssetUpdateCore();
        }
        finally
        {
            Interlocked.Exchange(ref assetUpdateRunning, 0);
        }
    }

    static string GetResName(string resType) => resType switch
    {
        "money" => "دلار (پول)",
        "iron" => "تن آهن",
        "soldiers" => "سرباز",
        "tanks" => "دستگاه تانک",
        "planes" => "فروند جنگنده",
        "bombers" => "فروند بمب‌افکن",
        _ => resType
    };

    static async Task ProcessActiveTransfers(CancellationToken ct)
    {
        var transfers = Database.GetActiveTransfers();
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var t in transfers)
        {
            var receiver = Database.GetCountry(t.ReceiverId, t.ChatId);
            var sender = Database.GetCountry(t.SenderId, t.ChatId);
            string sName = sender?.OwnerName ?? $"کاربر {t.SenderId}";
            string rName = receiver?.OwnerName ?? $"کاربر {t.ReceiverId}";
            string rn = GetResName(t.ResourceType);
            if (t.ArriveAtMs <= now)
            {
                if (receiver != null)
                {
                    switch (t.ResourceType)
                    {
                        case "money": receiver.Money += t.Amount; break;
                        case "iron": receiver.Iron += t.Amount; break;
                        case "soldiers": receiver.Soldiers += t.Amount; break;
                        case "tanks": receiver.Tanks += t.Amount; Database.AddEquipmentModel(t.ReceiverId, t.ChatId, "Tanks", Database.GetDefaultTankModel(sender?.Faction ?? receiver.Faction), t.Amount); break;
                        case "planes": receiver.Planes += t.Amount; Database.AddEquipmentModel(t.ReceiverId, t.ChatId, "Planes", Database.GetDefaultPlaneModel(sender?.Faction ?? receiver.Faction), t.Amount); break;
                        case "bombers": receiver.Bombers += t.Amount; Database.AddEquipmentModel(t.ReceiverId, t.ChatId, "Bombers", Database.GetDefaultBomberModel(sender?.Faction ?? receiver.Faction), t.Amount); break;
                    }
                    Database.UpdateCountryFull(receiver);
                    Database.ReconcileDefense(t.ReceiverId, t.ChatId);
                    Database.DeleteTransfer(t.Id);
                    try { await bot.SendTextMessageAsync(t.ReceiverId, $"📦 محموله رسید!\n{t.Amount:N0} {rn} از {sName}", cancellationToken: ct); } catch { }
                    try { await bot.SendTextMessageAsync(t.SenderId, $"✅ محموله به {rName} تحویل شد.", cancellationToken: ct); } catch { }
                }
                else
                {
                    if (sender != null)
                    {
                        switch (t.ResourceType)
                        {
                            case "money": sender.Money += t.Amount; break;
                            case "iron": sender.Iron += t.Amount; break;
                            case "soldiers": sender.Soldiers += t.Amount; break;
                            case "tanks": sender.Tanks += t.Amount; break;
                            case "planes": sender.Planes += t.Amount; break;
                            case "bombers": sender.Bombers += t.Amount; break;
                        }
                        Database.UpdateCountryFull(sender);
                        Database.ReconcileDefense(t.SenderId, t.ChatId);
                        try { await bot.SendTextMessageAsync(t.SenderId, $"↩️ محموله برگشت خورد! گیرنده کشورش را از دست داده بود. {t.Amount:N0} {rn} به انبارت برگشت.", cancellationToken: ct); } catch { }
                    }
                    Database.DeleteTransfer(t.Id);
                }
            }
            else if ((t.ArriveAtMs - now) <= 5 * 60 * 1000 && t.Notified == 0)
            {
                Database.UpdateTransferNotified(t.Id, 1);
                try { await bot.SendTextMessageAsync(t.ReceiverId, $"⏳ محموله از {sName} تا ۵ دقیقه دیگر!", cancellationToken: ct); } catch { }
            }
        }
    }
    static async Task ProcessActiveDeployments(CancellationToken ct)
    {
        var deployments = Database.GetActiveDeployments();
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var d in deployments)
        {
            var alliance = Database.GetAllianceById(d.AllianceId);
            string aName = alliance?.Name ?? "اتحاد";
            var tc = Database.GetCountry(d.TargetUserId, d.ChatId);
            string tName = tc?.Name ?? $"کاربر {d.TargetUserId}";
            if (d.EndAtMs <= now)
            {
                // FIX(2): در پایان طبیعی صف‌آرایی هم پیام پین‌شده آنپین و حذف شود
                await UnpinAndDeleteAnnounce(d.ChatId, d.AnnounceMsgId, ct);

                string gTitle = $"گروه {d.ChatId}";
                try { var ch = await bot.GetChatAsync(d.ChatId, ct); if (!string.IsNullOrEmpty(ch.Title)) gTitle = ch.Title; } catch { }
                if (d.Type == "Offensive")
                {
                    var attacker = Database.GetCountry(d.InitiatorId, d.ChatId);
                    var defender = tc;
                    if (attacker == null || defender == null) { Database.DeleteDeployment(d.Id); try { await SendPermanent(d.ChatId, $"❌ صف‌آرایی «{aName}» علیه «{tName}» لغو شد.", ct: ct); } catch { } continue; }
                    try { await SendPermanent(d.ChatId, $"⚔️ آغاز تهاجم اتحاد «{aName}» علیه «{tName}»!", ct: ct); } catch { }
                    attacker.Tanks += d.Tanks; attacker.Soldiers += d.Soldiers; attacker.Planes += d.Fighters; attacker.Bombers += d.Bombers;
                    var result = WarEngine.RunBattle(attacker, defender, d.Tanks, d.Soldiers, d.Fighters, d.Bombers, d.Strategy, d.Tactic, 0, 0);
                    attacker.Tanks = Math.Max(0, attacker.Tanks - d.Tanks);
                    attacker.Soldiers = Math.Max(0, attacker.Soldiers - d.Soldiers);
                    attacker.Planes = Math.Max(0, attacker.Planes - d.Fighters);
                    attacker.Bombers = Math.Max(0, attacker.Bombers - d.Bombers);
                    double tR = d.Tanks > 0 ? (double)Math.Max(0, d.Tanks - result.AttackerTanksLost) / d.Tanks : 0;
                    double sR = d.Soldiers > 0 ? (double)Math.Max(0, d.Soldiers - result.AttackerSoldiersLost) / d.Soldiers : 0;
                    double fR = d.Fighters > 0 ? (double)Math.Max(0, d.Fighters - result.AttackerFightersLost) / d.Fighters : 0;
                    double bR = d.Bombers > 0 ? (double)Math.Max(0, d.Bombers - result.AttackerBombersLost) / d.Bombers : 0;
                    var contribs = Database.GetDeploymentContributors(d.Id);
                    foreach (var cbn in contribs)
                    {
                        var cc = Database.GetCountry(cbn.UserId, d.ChatId);
                        if (cc != null)
                        {
                            cc.Tanks += (long)Math.Round(cbn.Tanks * tR);
                            cc.Soldiers += (long)Math.Round(cbn.Soldiers * sR);
                            cc.Planes += (long)Math.Round(cbn.Fighters * fR);
                            cc.Bombers += (long)Math.Round(cbn.Bombers * bR);
                            if (cbn.UserId == d.InitiatorId) { cc.Money += result.AttackerMoneyGained; cc.Iron += result.AttackerIronGained; cc.Welfare += result.AttackerWelfareChange; }
                            Database.UpdateCountryFull(cc);
                            Database.ReconcileDefense(cbn.UserId, d.ChatId);
                        }
                    }
                    if (!contribs.Any(x => x.UserId == d.InitiatorId))
                    {
                        var initC = Database.GetCountry(d.InitiatorId, d.ChatId);
                        if (initC != null) { initC.Money += result.AttackerMoneyGained; initC.Iron += result.AttackerIronGained; initC.Welfare += result.AttackerWelfareChange; Database.UpdateCountryFull(initC); Database.ReconcileDefense(initC.OwnerId, d.ChatId); }
                    }
                    defender.Tanks = Math.Max(0, defender.Tanks - result.DefenderTanksLost);
                    defender.Soldiers = Math.Max(0, defender.Soldiers - result.DefenderSoldiersLost);
                    defender.Planes = Math.Max(0, defender.Planes - result.DefenderFightersLost);
                    defender.AntiAir = Math.Max(0, defender.AntiAir - result.DefenderAntiAirLost);
                    defender.Money = Math.Max(0, defender.Money - result.DefenderMoneyLost);
                    defender.Iron = Math.Max(0, defender.Iron - result.DefenderIronLost);
                    defender.Welfare += result.DefenderWelfareChange;
                    Database.UpdateCountryFull(defender);
                    Database.ReconcileDefense(defender.OwnerId, d.ChatId);
                    if (!string.IsNullOrEmpty(result.AttackerReport))
                    {
                        var allIds = contribs.Select(x => x.UserId).Distinct().ToList();
                        foreach (var pid in allIds) { try { await bot.SendTextMessageAsync(pid, result.AttackerReport, cancellationToken: ct); } catch { } }
                    }
                    if (!string.IsNullOrEmpty(result.DefenderReport)) try { await bot.SendTextMessageAsync(d.TargetUserId, result.DefenderReport, cancellationToken: ct); } catch { }
                    if (!string.IsNullOrEmpty(result.GroupAnnouncement)) { try { await SendPermanent(d.ChatId, result.GroupAnnouncement, ct: ct); } catch { } }
                    await ProcessSiege(d.InitiatorId, d.TargetUserId, d.ChatId, result, ct);
                    Database.DeleteDeployment(d.Id);
                }
                else
                {
                    var tcDef = Database.GetCountry(d.TargetUserId, d.ChatId);
                    if (tcDef != null) { tcDef.Tanks = Math.Max(0, tcDef.Tanks - d.Tanks); tcDef.Soldiers = Math.Max(0, tcDef.Soldiers - d.Soldiers); tcDef.Planes = Math.Max(0, tcDef.Planes - d.Fighters); tcDef.Bombers = Math.Max(0, tcDef.Bombers - d.Bombers); tcDef.DefenseTanks = Math.Max(0, tcDef.DefenseTanks - d.Tanks); tcDef.DefenseSoldiers = Math.Max(0, tcDef.DefenseSoldiers - d.Soldiers); tcDef.DefenseFighters = Math.Max(0, tcDef.DefenseFighters - d.Fighters); Database.UpdateCountryFull(tcDef); Database.ReconcileDefense(tcDef.OwnerId, d.ChatId); }
                    var defC = Database.GetDeploymentContributors(d.Id);
                    long oT = defC.Sum(x => x.Tanks), oS = defC.Sum(x => x.Soldiers), oF = defC.Sum(x => x.Fighters), oB = defC.Sum(x => x.Bombers);
                    double tr = oT > 0 ? (double)Math.Max(0, d.Tanks) / oT : 1.0;
                    double sr = oS > 0 ? (double)Math.Max(0, d.Soldiers) / oS : 1.0;
                    double fr = oF > 0 ? (double)Math.Max(0, d.Fighters) / oF : 1.0;
                    double br = oB > 0 ? (double)Math.Max(0, d.Bombers) / oB : 1.0;
                    foreach (var cbn in defC)
                    {
                        var cc = Database.GetCountry(cbn.UserId, d.ChatId);
                        if (cc != null) { cc.Tanks += (long)Math.Round(cbn.Tanks * tr); cc.Soldiers += (long)Math.Round(cbn.Soldiers * sr); cc.Planes += (long)Math.Round(cbn.Fighters * fr); cc.Bombers += (long)Math.Round(cbn.Bombers * br); Database.UpdateCountryFull(cc); Database.ReconcileDefense(cbn.UserId, d.ChatId); }
                    }
                    Database.DeleteDeployment(d.Id);
                    try { await SendPermanent(d.ChatId, $"🛡 پایان دفاع اتحاد «{aName}» از «{tName}»", ct: ct); } catch { }
                }
            }
            else if (d.Type == "Offensive" && (now - d.LastWarnMs) >= 30 * 60 * 1000 && d.LastWarnMs > 0)
            {
                Database.UpdateDeploymentWarnMs(d.Id, now);
                try { await bot.SendTextMessageAsync(d.TargetUserId, $"⚠️ هشدار: صف‌آرایی «{aName}» علیه شما — {FormatRemaining(d.EndAtMs - now)} دیگر", cancellationToken: ct); } catch { }
            }
        }
    }

    static async Task RunAssetUpdateCore()
    {
        try { await ProcessActiveTransfers(CancellationToken.None); } catch (Exception ex) { Console.WriteLine($"[Transfers ERR] {ex.Message}"); }
        try { await ProcessActiveDeployments(CancellationToken.None); } catch (Exception ex) { Console.WriteLine($"[Deployments ERR] {ex.Message}"); }
        attackCounts.Clear();
        transferCounts.Clear();
        lastAssetUpdateAt = DateTime.UtcNow;
        var countries = Database.GetAllCountries();
        Console.WriteLine($"[TIMER] RunAssetUpdate started at {DateTime.Now} — {countries.Count} countries");
        foreach (var c in countries)
        {
            double sf = SiegeIncomeFactor(c);
            long moneyGain = (long)(CalcBuildingMoney(c) * sf);
            long ironGain = (long)(CalcIronIncome(c) * sf);
            long taxGain = (long)(CalcTaxIncome(c) * sf);
            double birthRate = c.Welfare / 100.0 * 0.05;
            long births = (long)(c.Population * birthRate);
            long newPop = c.Population + births;
            long newSol = c.Soldiers + (long)(births * c.RecruitmentRate / 10.0);
            double newWelfare = NextWelfare(c);
            c.Money += moneyGain + taxGain;
            c.Iron += ironGain;
            c.Population = newPop;
            c.Soldiers = newSol;
            c.Welfare = newWelfare;
            Database.UpdateCountryFull(c);
            Database.ReconcileDefense(c.OwnerId, c.ChatId);
        }

        string updateCaption =
            "🌅 گزارش روزانهٔ کشورها\n\n" +
            "💰 مالیات و درآمد ساختمان‌ها به خزانه واریز شد\n" +
            "👥 جمعیت بر اساس رفاه رشد کرد\n" +
            "🪖 سربازگیری طبق نرخ انجام شد\n" +
            "🏥 رفاه بر اساس مالیات، سربازگیری و بندر به‌روزرسانی شد\n\n" +
            "📊 برای مشاهدهٔ جزئیات بنویسید: کشورم";
        var chatIds = countries.Select(x => x.ChatId).Distinct().ToList();
        int sentGroups = 0;
        int failedGroups = 0;
        foreach (var cid in chatIds)
        {
            bool sent = false;
            for (int attempt = 0; attempt < 2 && !sent; attempt++)
            {
                try
                {
                    if (!string.IsNullOrEmpty(SpecialPhotoFileId))
                        await SendPermanentPhoto(cid, SpecialPhotoFileId, updateCaption, ct: CancellationToken.None);
                    else
                        await SendPermanent(cid, updateCaption, ct: CancellationToken.None);
                    sent = true;
                    sentGroups++;
                }
                catch (Telegram.Bot.Exceptions.ApiRequestException apiEx) when (apiEx.ErrorCode == 429)
                {
                    int waitSec = apiEx.Parameters?.RetryAfter ?? 3;
                    Console.WriteLine($"[UPDATE SEND ERR] chat {cid}: flood control, waiting {waitSec}s");
                    await Task.Delay(waitSec * 1000 + 200);
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(SpecialPhotoFileId))
                    {
                        try
                        {
                            await SendPermanent(cid, updateCaption, ct: CancellationToken.None);
                            sent = true;
                            sentGroups++;
                            Console.WriteLine($"[UPDATE SEND ERR] chat {cid}: photo failed ({ex.Message}), fell back to text");
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine($"[UPDATE SEND ERR] chat {cid}: {ex2.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[UPDATE SEND ERR] chat {cid}: {ex.Message}");
                    }
                    break;
                }
            }
            if (!sent) failedGroups++;
            await Task.Delay(60);
        }
        Console.WriteLine($"[TIMER] Update sent to {sentGroups} groups, failed {failedGroups}");

        try
        {
            string backupPath = $"gamedata_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            System.IO.File.Copy("gamedata.db", backupPath, overwrite: true);
            await bot.SendDocumentAsync(OWNER_ID,
                new InputOnlineFile(System.IO.File.OpenRead(backupPath), backupPath),
                caption: $"📦 بک‌آپ دیتابیس — {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n👥 تعداد کشورها: {countries.Count}",
                cancellationToken: CancellationToken.None);
            Console.WriteLine($"[TIMER] DB backup sent to owner");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BACKUP ERR] {ex.Message}");
            try
            {
                await bot.SendDocumentAsync(OWNER_ID,
                    new InputOnlineFile(System.IO.File.OpenRead("gamedata.db"), "gamedata.db"),
                    caption: "📦 دیتابیس (fallback)",
                    cancellationToken: CancellationToken.None);
            }
            catch { }
        }
    }

    static async Task HandleAttackGroupCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
{
        if (Database.HasAttackAbandonLock(cb.From.Id)) { await bot.AnswerCallbackQueryAsync(cb.Id, "⛔ شما تا ۳ روز به دلیل بزن‌دررو از حمله قفل هستید.", showAlert: true, cancellationToken: ct); return; }
        if (parts.Length < 2 || cb.Message == null) return;
        long uid = cb.From.Id;
        if (!TryParseLong(parts[1], out long cid)) return;
        var targets = Database.GetCountriesByChatId(cid).Where(c => c.OwnerId != uid).ToList();
        if (targets.Count == 0) { await bot.AnswerCallbackQueryAsync(cb.Id, "هدف نیست.", cancellationToken: ct); return; }
        var kb = targets.Select(t => new[] { InlineKeyboardButton.WithCallbackData(t.OwnerName, $"attack_target:{cid}:{t.OwnerId}") }).ToArray();
        sessions[uid] = new UserSession { Step = SessionStep.AttackWaitingTarget, AttackChatId = cid };
        await bot.EditMessageTextAsync(cb.Message.Chat.Id, cb.Message.MessageId, "🎯 هدف:", replyMarkup: new InlineKeyboardMarkup(kb), cancellationToken: ct);
        TrackPrompt(uid, cb.Message.Chat.Id, cb.Message.MessageId);
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
    }

    static async Task HandleAttackTargetCallback(
        CallbackQuery cb,
        string[] parts,
        CancellationToken ct)
{
        if (Database.HasAttackAbandonLock(cb.From.Id)) { await bot.AnswerCallbackQueryAsync(cb.Id, "⛔ شما تا ۳ روز به دلیل بزن‌دررو از حمله قفل هستید.", showAlert: true, cancellationToken: ct); return; }
        if (parts.Length < 3 || cb.Message == null)
            return;

        long uid = cb.From.Id;

        if (!TryParseLong(parts[1], out long cid) ||
            !TryParseLong(parts[2], out long tid))
            return;

        var defender = Database.GetCountry(tid, cid);

        if (defender == null)
        {
            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                "❌ هدف یافت نشد.",
                cancellationToken: ct
            );
            return;
        }

        var session = sessions.GetOrAdd(
            uid,
            _ => new UserSession()
        );

        session.Step = SessionStep.AttackWaitingStrategy;
        session.AttackChatId = cid;
        session.AttackTargetId = tid;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "⚔️ هجوم منسجم",
                    $"attack_strategy:{cid}:{tid}:1"
                )
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "⭕ محاصره و ضربه",
                    $"attack_strategy:{cid}:{tid}:2"
                )
            }
        });

        string text =
            $"🎯 هدف: {defender.Name}\n\n" +
            GroundAttackStrategyGuide;

        await bot.EditMessageTextAsync(
            cb.Message.Chat.Id,
            cb.Message.MessageId,
            text,
            replyMarkup: keyboard,
            cancellationToken: ct
        );

        TrackPrompt(
            uid,
            cb.Message.Chat.Id,
            cb.Message.MessageId
        );

        await bot.AnswerCallbackQueryAsync(
            cb.Id,
            cancellationToken: ct
        );
    }

    static async Task HandleAttackStrategyCallback(
        CallbackQuery cb,
        string[] parts,
        CancellationToken ct)
{
        if (Database.HasAttackAbandonLock(cb.From.Id)) { await bot.AnswerCallbackQueryAsync(cb.Id, "⛔ شما تا ۳ روز به دلیل بزن‌دررو از حمله قفل هستید.", showAlert: true, cancellationToken: ct); return; }
        if (parts.Length < 4 || cb.Message == null)
            return;

        long uid = cb.From.Id;

        if (!TryParseLong(parts[1], out long cid) ||
            !TryParseLong(parts[2], out long tid) ||
            !TryParseInt(parts[3], out int strategy) ||
            strategy is < 1 or > 2)
            return;

        var session = sessions.GetOrAdd(
            uid,
            _ => new UserSession()
        );

        session.Step = SessionStep.AttackWaitingTactic;
        session.AttackChatId = cid;
        session.AttackTargetId = tid;
        session.AttackStrategy = strategy;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GroundAttackTacticName(strategy, 1),
                    $"attack_tactic:{cid}:{tid}:{strategy}:1"
                )
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GroundAttackTacticName(strategy, 2),
                    $"attack_tactic:{cid}:{tid}:{strategy}:2"
                )
            }
        });

        await bot.EditMessageTextAsync(
            cb.Message.Chat.Id,
            cb.Message.MessageId,
            GroundAttackTacticGuide(strategy),
            replyMarkup: keyboard,
            cancellationToken: ct
        );

        TrackPrompt(
            uid,
            cb.Message.Chat.Id,
            cb.Message.MessageId
        );

        await bot.AnswerCallbackQueryAsync(
            cb.Id,
            cancellationToken: ct
        );
    }

    static async Task HandleAttackTacticCallback(
        CallbackQuery cb,
        string[] parts,
        CancellationToken ct)
    {
        if (parts.Length < 5 || cb.Message == null)
            return;

        long uid = cb.From.Id;

        if (!TryParseLong(parts[1], out long cid) ||
            !TryParseLong(parts[2], out long tid) ||
            !TryParseInt(parts[3], out int strategy) ||
            !TryParseInt(parts[4], out int tactic))
            return;

        var session = sessions.GetOrAdd(
            uid,
            _ => new UserSession()
        );

        session.AttackChatId = cid;
        session.AttackTargetId = tid;
        session.AttackStrategy = strategy;
        session.AttackTactic = tactic;

        var attacker = Database.GetCountry(uid, cid);

        if (attacker == null)
        {
            EndSession(uid);

            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                "❌ کشور مهاجم یافت نشد.",
                showAlert: true,
                cancellationToken: ct
            );
            return;
        }

        string forcePrompt;

        if (attacker.Tanks <= 0)
        {
            session.AttackTanks = 0;
            session.Step = SessionStep.AttackWaitingSoldiers;

            forcePrompt =
                "🪖 تعداد سربازان اعزامی را وارد کنید.\n" +
                InventoryLine(attacker.Soldiers);
        }
        else
        {
            session.Step = SessionStep.AttackWaitingTanks;

            forcePrompt =
                "🛡 تعداد تانک‌های اعزامی را وارد کنید.\n" +
                InventoryLine(attacker.Tanks);
        }

        await bot.EditMessageTextAsync(
            cb.Message.Chat.Id,
            cb.Message.MessageId,
            forcePrompt,
            replyMarkup: null,
            cancellationToken: ct
        );

        TrackPrompt(
            uid,
            cb.Message.Chat.Id,
            cb.Message.MessageId
        );

        await bot.AnswerCallbackQueryAsync(
            cb.Id,
            cancellationToken: ct
        );
    }

    static async Task HandleAttackAirStrategyCallback(
        CallbackQuery cb,
        string[] parts,
        CancellationToken ct)
    {
        if (parts.Length < 2 || cb.Message == null)
            return;

        long uid = cb.From.Id;

        if (!TryParseInt(parts[1], out int strategy) ||
            strategy is < 1 or > 2)
            return;

        if (!sessions.TryGetValue(uid, out var session) ||
            session == null)
        {
            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                cancellationToken: ct
            );
            return;
        }

        session.AttackAirStrategy = strategy;
        session.Step = SessionStep.AttackWaitingAirTactic;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    AirAttackTacticName(strategy, 1),
                    "attack_air_tactic:1"
                )
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    AirAttackTacticName(strategy, 2),
                    "attack_air_tactic:2"
                )
            }
        });

        await bot.EditMessageTextAsync(
            cb.Message.Chat.Id,
            cb.Message.MessageId,
            AirAttackTacticGuide(strategy),
            replyMarkup: keyboard,
            cancellationToken: ct
        );

        TrackPrompt(
            uid,
            cb.Message.Chat.Id,
            cb.Message.MessageId
        );

        await bot.AnswerCallbackQueryAsync(
            cb.Id,
            cancellationToken: ct
        );
    }

    static async Task HandleAttackAirTacticCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || cb.Message == null) return;
        long uid = cb.From.Id;
        if (!TryParseInt(parts[1], out int aTac)) return;
        if (!sessions.TryGetValue(uid, out var sess) || sess == null) { await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct); return; }
        sess.AttackAirTactic = aTac;
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
        DeleteNow(cb.Message.Chat.Id, cb.Message.MessageId);
        await RunAttackBattle(uid, sess, ct);
    }

    static async Task RunAttackBattle(long uid, UserSession sess, CancellationToken ct)
{
        if (Database.HasAttackAbandonLock(uid))
        {
            var lkUntil = DateTimeOffset.FromUnixTimeMilliseconds(Database.GetAttackAbandonLockUntilMs(uid)).ToOffset(TimeSpan.FromHours(3.5));
            await SendTemp(uid, $"🔒 کشور شما به دلیل انصراف و حذف کشور پس از حمله (بزن‌دررو)، تا <b>{lkUntil:yyyy/MM/dd HH:mm}</b> (تهران) در همه گروه‌ها از حمله کردن قفل است.", parseMode: ParseMode.Html, ct: ct);
            return;
        }
        var attacker = Database.GetCountry(uid, sess.AttackChatId);
        var defender = Database.GetCountry(sess.AttackTargetId, sess.AttackChatId);
        long cid = sess.AttackChatId;
        long tid = sess.AttackTargetId;
        int aStr = sess.AttackStrategy; int aTac = sess.AttackTactic;
        long aTnk = sess.AttackTanks; long aSol = sess.AttackSoldiers;
        long aFig = sess.AttackFighters; long aBom = sess.AttackBombers;
        int aAirStr = sess.AttackAirStrategy; int aAirTac = sess.AttackAirTactic;
        EndSession(uid);
        if (attacker == null || defender == null) { await SendTemp(uid, "❌ کشور یافت نشد.", ct: ct); return; }
        if (lastAssetUpdateAt != DateTime.MinValue)
        {
            double sinceMin = (DateTime.UtcNow - lastAssetUpdateAt).TotalMinutes;
            if (sinceMin < ATTACK_LOCK_MINUTES && !Database.HasGroupLockExemption(cid))
            {
                int left = (int)Math.Ceiling(ATTACK_LOCK_MINUTES - sinceMin);
                await SendTemp(uid, $"⛔ تا {left} دقیقه دیگر حمله ممکن نیست.", ct: ct);
                return;
            }
        }
        if (GetAttackCount(cid, uid) >= MAX_ATTACKS_PER_UPDATE && !Database.HasGroupLockExemption(cid))
        { await SendTemp(uid, $"⛔ سهمیه تمام شد ({MAX_ATTACKS_PER_UPDATE}).", ct: ct); return; }
        if (defender.CreatedAtMs > 0 && !Database.HasShieldExemption(defender.OwnerId, defender.ChatId) && !Database.HasGroupLockExemption(defender.ChatId))
        {
            double ageH = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - defender.CreatedAtMs) / 3600000.0;
            if (ageH < SHIELD_HOURS) { int leftH = (int)Math.Ceiling(SHIELD_HOURS - ageH); await SendTemp(uid, $"🛡 سپر! {leftH} ساعت دیگر", ct: ct); return; }
        }
        IncAttackCount(cid, uid);
        // FIX(3): ثبت حمله دریافتی برای مدافع
        string todayTehranInc = DateTime.UtcNow.AddHours(3.5).ToString("yyyy-MM-dd");
        Database.IncDailyDefendCount(defender.OwnerId, todayTehranInc);
        // FIX(1b): ثبت اینکه حمله‌کننده امروز حمله واقعی زده
        Database.SetAttackerFlag(uid, todayTehranInc);
        await SendTemp(uid, "⚔️ در حال پردازش نبرد...", ct: ct);
        var result = WarEngine.RunBattle(attacker, defender, aTnk, aSol, aFig, aBom, aStr, aTac, aAirStr, aAirTac);
        attacker.Tanks = Math.Max(0, attacker.Tanks - result.AttackerTanksLost);
        attacker.Soldiers = Math.Max(0, attacker.Soldiers - result.AttackerSoldiersLost);
        attacker.Planes = Math.Max(0, attacker.Planes - result.AttackerFightersLost);
        attacker.Bombers = Math.Max(0, attacker.Bombers - result.AttackerBombersLost);
        attacker.Money += result.AttackerMoneyGained;
        attacker.Iron += result.AttackerIronGained;
        attacker.Welfare += result.AttackerWelfareChange;
        var defDep = Database.GetActiveDeployments().FirstOrDefault(d => d.ChatId == cid && d.Type == "Defensive" && d.TargetUserId == defender.OwnerId);
        if (defDep != null)
        {
            if (defender.Tanks > 0 && defDep.Tanks > 0) defDep.Tanks = Math.Max(0, defDep.Tanks - (long)Math.Round((double)defDep.Tanks / defender.Tanks * result.DefenderTanksLost));
            if (defender.Soldiers > 0 && defDep.Soldiers > 0) defDep.Soldiers = Math.Max(0, defDep.Soldiers - (long)Math.Round((double)defDep.Soldiers / defender.Soldiers * result.DefenderSoldiersLost));
            if (defender.Planes > 0 && defDep.Fighters > 0) defDep.Fighters = Math.Max(0, defDep.Fighters - (long)Math.Round((double)defDep.Fighters / defender.Planes * result.DefenderFightersLost));
            Database.UpdateDeploymentForces(defDep);
        }
        defender.Tanks = Math.Max(0, defender.Tanks - result.DefenderTanksLost);
        defender.Soldiers = Math.Max(0, defender.Soldiers - result.DefenderSoldiersLost);
        defender.Planes = Math.Max(0, defender.Planes - result.DefenderFightersLost);
        defender.AntiAir = Math.Max(0, defender.AntiAir - result.DefenderAntiAirLost);
        defender.Money = Math.Max(0, defender.Money - result.DefenderMoneyLost);
        defender.Iron = Math.Max(0, defender.Iron - result.DefenderIronLost);
        defender.Welfare += result.DefenderWelfareChange;
        Database.UpdateCountryFull(attacker);
        Database.UpdateCountryFull(defender);
        Database.ReconcileDefense(attacker.OwnerId, attacker.ChatId);
        Database.ReconcileDefense(defender.OwnerId, defender.ChatId);
        if (!string.IsNullOrEmpty(result.AttackerReport)) await SendPermanent(uid, result.AttackerReport, ct: ct);
        if (!string.IsNullOrEmpty(result.DefenderReport)) { try { await SendPermanent(tid, result.DefenderReport, ct: ct); } catch { } }
        if (!string.IsNullOrEmpty(result.GroupAnnouncement)) { try { await SendPermanent(cid, result.GroupAnnouncement, ct: ct); } catch { } }
        await ProcessSiege(uid, tid, cid, result, ct);
    }

    static async Task ProcessSiege(long attackerId, long defenderId, long chatId, BattleResult result, CancellationToken ct)
    {
        var atkAfter = Database.GetCountry(attackerId, chatId);
        var def = Database.GetCountry(defenderId, chatId);
        if (def == null) return;
        bool routDefeat = result.AttackerWon && atkAfter != null && atkAfter.Soldiers >= 5000 && atkAfter.Tanks >= 50;
        string atkName = atkAfter?.Name ?? "دشمن";
        if (routDefeat)
        {
            Database.SetDefenseWins(defenderId, chatId, 0);
            int cnt = Database.AddRoutDefeat(defenderId, chatId, attackerId, +1);
            if (cnt % 5 == 0)
            {
                int newCities = Math.Max(0, def.Cities - 1);
                Database.SetCities(defenderId, chatId, newCities);
                bool gained = Database.AddCityToAttacker(attackerId, chatId);
                string gm = gained ? "" : "\n(سقف ۲۰ شهر)";
                if (cnt == 5 && def.Besieged < 1) Database.SetBesieged(defenderId, chatId, 1);
                try { await SendPermanent(chatId, $"💥 شهر {def.Name} به تصرف {atkName} درآمد! (باقی: {newCities}){gm}", ct: ct); } catch { }
                if (newCities <= 0)
                {
                    Database.DeleteCountry(defenderId, chatId);
                    try { await SendPermanent(defenderId, $"☠️ کشورتان به‌دست {atkName} سقوط کرد.", ct: ct); } catch { }
                    try { await SendPermanent(chatId, $"☠️ {def.Name} سقوط کرد!", ct: ct); } catch { }
                    return;
                }
                if (newCities <= 1 && def.Besieged < 2) { Database.SetBesieged(defenderId, chatId, 2); try { await SendPermanent(chatId, $"🆘 {def.Name} بحرانی!", ct: ct); } catch { } }
            }
            else
            {
                int nextMilestone = ((cnt / 5) + 1) * 5;
                int left = nextMilestone - cnt;
                await SendPermanent(defenderId, $"🚨 شکست! شماره شکست: {cnt}\n⚠️ {left} شکست تا سقوط شهر بعدی!", ct: ct);
            }
        }
        else if (result.AttackerFailed || result.SuccessPercent < 40)
        {
            if (def.Besieged >= 1)
            {
                int wins = def.DefenseWins + 1;
                if (wins >= 5)
                {
                    wins = 0;
                    int newCities = Math.Min(Database.MAX_CITIES, def.Cities + 1);
                    Database.SetCities(defenderId, chatId, newCities);
                    Database.AddRoutDefeat(defenderId, chatId, attackerId, -5);
                    int newState = def.Besieged;
                    if (def.Besieged == 2 && newCities > 1) newState = 1;
                    if (Database.MaxRoutDefeats(defenderId, chatId) < 5) newState = 0;
                    if (newState != def.Besieged) Database.SetBesieged(defenderId, chatId, newState);
                    await SendPermanent(defenderId, $"🎉 شهر بازپس گرفته شد! (شهرها: {newCities})", ct: ct);
                    try { await SendPermanent(chatId, $"🎌 {def.Name} شهر را پس گرفت! ({newCities})", ct: ct); } catch { }
                }
                else { Database.SetDefenseWins(defenderId, chatId, wins); await SendPermanent(defenderId, $"🛡 دفاع موفق! ({wins}/5)", ct: ct); }
            }
            else
            {
                int before = Database.GetRoutDefeats(defenderId, chatId, attackerId);
                if (before > 0) { int cnt = Database.AddRoutDefeat(defenderId, chatId, attackerId, -1); await SendPermanent(defenderId, $"🛡 فشار کاهش یافت. شکست: {cnt}", ct: ct); }
            }
        }
    }

    static async Task SendDefenseStatus(
        long sendTo,
        long ownerId,
        long chatId,
        CancellationToken ct)
    {
        Database.ReconcileDefense(ownerId, chatId);

        var country = Database.GetCountry(ownerId, chatId);

        if (country == null)
        {
            await SendTemp(
                sendTo,
                "❌ کشور یافت نشد.",
                ct: ct
            );
            return;
        }

        long minimumTanks =
            (long)Math.Ceiling(country.Tanks * 0.2);

        long minimumSoldiers =
            (long)Math.Ceiling(country.Soldiers * 0.2);

        string groundStrategy =
            GroundDefenseStrategyName(
                country.DefenseStrategy
            );

        string groundTactic =
            GroundDefenseTacticName(
                country.DefenseStrategy,
                country.DefenseTactic
            );

        string airStrategy =
            AirDefenseStrategyName(
                country.AirDefStrategy
            );

        string airTactic =
            AirDefenseTacticName(
                country.AirDefStrategy,
                country.AirDefTactic
            );

        string text =
            $"🛡 وضعیت دفاع {country.Name}\n\n" +

            "⚔️ دفاع زمینی\n" +
            $"استراتژی: {groundStrategy}\n" +
            $"تاکتیک: {groundTactic}\n\n" +

            "🛫 دفاع هوایی\n" +
            $"استراتژی: {airStrategy}\n" +
            $"تاکتیک: {airTactic}\n\n" +

            "🛡 نیروهای مستقر در دفاع\n" +
            $"تانک: {country.DefenseTanks:N0}" +
            $" | حداقل: {minimumTanks:N0}\n" +
            $"سرباز: {country.DefenseSoldiers:N0}" +
            $" | حداقل: {minimumSoldiers:N0}\n" +
            $"جنگنده: {country.DefenseFighters:N0}\n" +
            $"پدافند: {country.AntiAir:N0}\n\n" +

            "📊 کل موجودی کشور\n" +
            $"تانک: {country.Tanks:N0}\n" +
            $"سرباز: {country.Soldiers:N0}\n" +
            $"جنگنده: {country.Planes:N0}";

        bool isPrivate = sendTo == ownerId;

        if (isPrivate)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "⚔️ تاکتیک زمینی",
                        $"defense_tactic:{chatId}"
                    )
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "🛫 دفاع هوایی",
                        $"airdef_strategy:{chatId}"
                    )
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "⚙️ انتخاب نیرو",
                        $"defense_set:{chatId}"
                    )
                }
            });

            await SendTemp(
                sendTo,
                text,
                markup: keyboard,
                ct: ct
            );
        }
        else
        {
            await SendTemp(
                sendTo,
                text + "\n\n⚙️ برای تنظیم به پیوی آلیس بروید.",
                ct: ct
            );
        }
    }

    static async Task HandleDefenseStatusCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2) return;
        long uid = cb.From.Id;
        if (!TryParseLong(parts[1], out long cid)) return;
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
        EndSession(uid);
        await SendDefenseStatus(uid, uid, cid, ct);
    }

    static readonly int[] DefensePercents = { 20, 30, 40, 50, 60, 70, 80, 90, 100 };
    static InlineKeyboardMarkup BuildPercentKeyboard(string kind, long chatId)
    {
        var rows = new List<InlineKeyboardButton[]>();
        for (int i = 0; i < DefensePercents.Length; i += 2)
        {
            var row = new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData($"{DefensePercents[i]}%", $"defense_pct:{chatId}:{kind}:{DefensePercents[i]}") };
            if (i + 1 < DefensePercents.Length) row.Add(InlineKeyboardButton.WithCallbackData($"{DefensePercents[i + 1]}%", $"defense_pct:{chatId}:{kind}:{DefensePercents[i + 1]}"));
            rows.Add(row.ToArray());
        }
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("❌ انصراف", "cancel") });
        return new InlineKeyboardMarkup(rows);
    }

    static async Task HandleDefenseSetCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 2 || cb.Message == null) return;
        long uid = cb.From.Id;
        if (!TryParseLong(parts[1], out long cid)) return;
        var c = Database.GetCountry(uid, cid);
        if (c == null) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌ کشور نیست!", cancellationToken: ct); return; }
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
        await bot.EditMessageTextAsync(cb.Message.Chat.Id, cb.Message.MessageId, $"🛡 درصد تانک:\nکل: {c.Tanks}", replyMarkup: BuildPercentKeyboard("tank", cid), cancellationToken: ct);
    }

    static async Task HandleDefensePctCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 4 || cb.Message == null) return;
        long uid = cb.From.Id;
        if (!TryParseLong(parts[1], out long cid) || !TryParseInt(parts[3], out int pct)) return;
        string kind = parts[2];
        var c = Database.GetCountry(uid, cid);
        if (c == null) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌", cancellationToken: ct); return; }
        if (kind == "tank")
        {
            long dt = (long)Math.Ceiling(c.Tanks * (pct / 100.0));
            sessions[uid] = new UserSession { Step = SessionStep.DefenseWaitingSoldiers, AttackChatId = cid, DefenseTanks = dt, DefTankPct = pct };
            await bot.AnswerCallbackQueryAsync(cb.Id, $"🛡 {pct}%", cancellationToken: ct);
            await bot.EditMessageTextAsync(cb.Message.Chat.Id, cb.Message.MessageId, $"🪖 درصد سرباز:\nکل: {c.Soldiers}", replyMarkup: BuildPercentKeyboard("soldier", cid), cancellationToken: ct);
            return;
        }
        if (kind == "soldier")
        {
            long defT = c.DefenseTanks; int dtp = 100;
            if (sessions.TryGetValue(uid, out var s) && s != null && s.AttackChatId == cid) { defT = s.DefenseTanks; dtp = s.DefTankPct > 0 ? s.DefTankPct : 100; }
            long ds = (long)Math.Ceiling(c.Soldiers * (pct / 100.0));
            sessions[uid] = new UserSession { Step = SessionStep.DefenseWaitingFighters, AttackChatId = cid, DefenseTanks = defT, DefenseSoldiers = ds, DefTankPct = dtp, DefSoldierPct = pct };
            await bot.AnswerCallbackQueryAsync(cb.Id, $"🪖 {pct}%", cancellationToken: ct);
            await bot.EditMessageTextAsync(cb.Message.Chat.Id, cb.Message.MessageId, $"✈️ درصد جنگنده:\nکل: {c.Planes}", replyMarkup: BuildPercentKeyboard("fighter", cid), cancellationToken: ct);
            return;
        }
        if (kind == "fighter")
        {
            long defT = c.DefenseTanks, defS = c.DefenseSoldiers;
            int dtp = 100, dsp = 100;
            if (sessions.TryGetValue(uid, out var s) && s != null && s.AttackChatId == cid) { defT = s.DefenseTanks; defS = s.DefenseSoldiers; dtp = s.DefTankPct > 0 ? s.DefTankPct : 100; dsp = s.DefSoldierPct > 0 ? s.DefSoldierPct : 100; }
            long df = (long)Math.Ceiling(c.Planes * (pct / 100.0));
            Database.UpdateDefenseFull(uid, cid, defT, defS, df, c.DefenseStrategy, c.DefenseTactic, dtp, dsp, pct);
            EndSession(uid);
            await bot.AnswerCallbackQueryAsync(cb.Id, $"✅ ذخیره شد.", cancellationToken: ct);
            DeleteNow(cb.Message.Chat.Id, cb.Message.MessageId);
            await SendDefenseStatus(uid, uid, cid, ct);
            return;
        }
        await bot.AnswerCallbackQueryAsync(cb.Id, cancellationToken: ct);
    }

    static async Task HandleDefenseTacticCallback(
        CallbackQuery cb,
        string[] parts,
        CancellationToken ct)
    {
        if (parts.Length < 2)
            return;

        long uid = cb.From.Id;

        if (!TryParseLong(parts[1], out long chatId))
            return;

        await bot.AnswerCallbackQueryAsync(
            cb.Id,
            cancellationToken: ct
        );

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🛡 دفاع منسجم",
                    $"defense_tactic_select:{chatId}:1"
                )
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "💥 دفاع و ضدحمله پراکنده",
                    $"defense_tactic_select:{chatId}:2"
                )
            }
        });

        await SendTemp(
            uid,
            GroundDefenseStrategyGuide,
            markup: keyboard,
            ct: ct
        );
    }

    static async Task HandleDefenseTacticSelectCallback(
        CallbackQuery cb,
        string[] parts,
        CancellationToken ct)
    {
        if (parts.Length < 3)
            return;

        long uid = cb.From.Id;

        if (!TryParseLong(parts[1], out long chatId) ||
            !TryParseInt(parts[2], out int strategy) ||
            strategy is < 1 or > 2)
            return;

        if (parts.Length >= 4 &&
            TryParseInt(parts[3], out int tactic))
        {
            if (tactic is < 1 or > 2)
                return;

            var country =
                Database.GetCountry(uid, chatId);

            if (country == null)
            {
                await bot.AnswerCallbackQueryAsync(
                    cb.Id,
                    "❌ کشور یافت نشد.",
                    cancellationToken: ct
                );
                return;
            }

            Database.UpdateDefense(
                uid,
                chatId,
                country.DefenseTanks,
                country.DefenseSoldiers,
                strategy,
                tactic
            );

            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                "✅ استراتژی و تاکتیک دفاعی ذخیره شد.",
                cancellationToken: ct
            );

            if (cb.Message != null)
            {
                DeleteNow(
                    cb.Message.Chat.Id,
                    cb.Message.MessageId
                );
            }

            await SendDefenseStatus(
                uid,
                uid,
                chatId,
                ct
            );
            return;
        }

        await bot.AnswerCallbackQueryAsync(
            cb.Id,
            cancellationToken: ct
        );

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GroundDefenseTacticName(strategy, 1),
                    $"defense_tactic_select:{chatId}:{strategy}:1"
                )
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    GroundDefenseTacticName(strategy, 2),
                    $"defense_tactic_select:{chatId}:{strategy}:2"
                )
            }
        });

        string guide =
            GroundDefenseTacticGuide(strategy);

        if (cb.Message != null)
        {
            await bot.EditMessageTextAsync(
                cb.Message.Chat.Id,
                cb.Message.MessageId,
                guide,
                replyMarkup: keyboard,
                cancellationToken: ct
            );
        }
        else
        {
            await SendTemp(
                uid,
                guide,
                markup: keyboard,
                ct: ct
            );
        }
    }

    static async Task HandleAirDefStrategyCallback(
        CallbackQuery cb,
        string[] parts,
        CancellationToken ct)
    {
        if (parts.Length < 2 || cb.Message == null)
            return;

        long uid = cb.From.Id;

        if (!TryParseLong(parts[1], out long chatId))
            return;

        if (parts.Length >= 3 &&
            TryParseInt(parts[2], out int strategy))
        {
            if (strategy is < 1 or > 2)
                return;

            await bot.AnswerCallbackQueryAsync(
                cb.Id,
                cancellationToken: ct
            );

            string tacticOne =
                AirDefenseTacticName(strategy, 1);

            string tacticTwo =
                AirDefenseTacticName(strategy, 2);

            if (strategy == 1)
                tacticTwo += " 🔒";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        tacticOne,
                        $"airdef_tactic:{chatId}:{strategy}:1"
                    )
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        tacticTwo,
                        $"airdef_tactic:{chatId}:{strategy}:2"
                    )
                }
            });

            await bot.EditMessageTextAsync(
                cb.Message.Chat.Id,
                cb.Message.MessageId,
                AirDefenseTacticGuide(strategy),
                replyMarkup: keyboard,
                cancellationToken: ct
            );
            return;
        }

        await bot.AnswerCallbackQueryAsync(
            cb.Id,
            cancellationToken: ct
        );

        var strategyKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🗺 دفاع منطقه‌ای",
                    $"airdef_strategy:{chatId}:1"
                )
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🎯 دفاع نقطه‌ای",
                    $"airdef_strategy:{chatId}:2"
                )
            }
        });

        await SendTemp(
            uid,
            AirDefenseStrategyGuide,
            markup: strategyKeyboard,
            ct: ct
        );
    }

    static async Task HandleAirDefTacticCallback(CallbackQuery cb, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 4 || cb.Message == null) return;
        long uid = cb.From.Id;
        if (!TryParseLong(parts[1], out long cid) || !TryParseInt(parts[2], out int str) || !TryParseInt(parts[3], out int tac)) return;
        if (str == 1 && tac == 2) { await bot.AnswerCallbackQueryAsync(cb.Id, "📡 رادار ندارید! قفل.", showAlert: true, cancellationToken: ct); return; }
        var c = Database.GetCountry(uid, cid);
        if (c == null) { await bot.AnswerCallbackQueryAsync(cb.Id, "❌", cancellationToken: ct); return; }
        c.AirDefStrategy = str; c.AirDefTactic = tac;
        Database.UpdateCountryFull(c);
        await bot.AnswerCallbackQueryAsync(cb.Id, "✅ ذخیره شد.", cancellationToken: ct);
        DeleteNow(cb.Message.Chat.Id, cb.Message.MessageId);
        await SendDefenseStatus(uid, uid, cid, ct);
    }
}

// ===== MERGED ADMIN CORE =====
sealed class AdminAccount
{
    public long AdminId { get; set; }
    public string DisplayName { get; set; } = "";
    public long AddedBy { get; set; }
    public bool IsOwner { get; set; }
    public bool IsActive { get; set; }
    public long CreatedAtMs { get; set; }
    public long LastSeenMs { get; set; }
}

sealed class AdminAuditEntry
{
    public long Id { get; set; }
    public long AdminId { get; set; }
    public string Action { get; set; } = "";
    public string TargetType { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string Details { get; set; } = "";
    public bool Success { get; set; }
    public long CreatedAtMs { get; set; }
}

sealed class AdminDashboardStats
{
    public int Countries { get; set; }
    public int Players { get; set; }
    public int Groups { get; set; }
    public int Alliances { get; set; }
    public int ActiveTransfers { get; set; }
    public int ActiveDeployments { get; set; }
    public int ActiveAdmins { get; set; }
    public int AuditEntries { get; set; }
}

sealed record AdminPermissionItem(
    string Code,
    string Title,
    string Category
);

static class AdminPermissionCatalog
{
    public static readonly AdminPermissionItem[] All =
    {
        new("DASH", "مشاهده داشبورد", "گزارش"),

        new("P_VIEW", "مشاهده پلیرها", "پلیر"),
        new("P_EDIT", "ویرایش اطلاعات پلیر", "پلیر"),
        new("P_BAN", "بن و رفع‌بن پلیر", "پلیر"),

        new("C_VIEW", "مشاهده کشورها", "کشور"),
        new("C_RES", "تغییر منابع اقتصادی", "کشور"),
        new("C_ARMY", "تغییر نیروهای نظامی", "کشور"),
        new("C_DELETE", "حذف کشور", "کشور"),

        new("G_VIEW", "مشاهده گروه‌ها", "گروه"),
        new("G_EDIT", "مدیریت گروه‌ها", "گروه"),
        new("ALLY", "مدیریت اتحادها", "گروه"),

        new("ROYAL", "مدیریت رویال‌کوین", "اقتصاد"),
        new("E_GLOBAL", "تنظیمات اقتصاد جهانی", "اقتصاد"),

        new("W_VIEW", "مشاهده وضعیت جنگ", "جنگ"),
        new("W_EDIT", "مدیریت جنگ و سپر", "جنگ"),

        new("O_VIEW", "مشاهده عملیات فعال", "عملیات"),
        new("O_EDIT", "مدیریت ترنسفر و صف‌آرایی", "عملیات"),

        new("ANN", "ارسال اعلامیه", "ارتباطات"),
        new("SET", "تغییر تنظیمات آلیس", "تنظیمات"),

        new("BACKUP", "دریافت بکاپ", "نگهداری"),
        new("RESTORE", "بازیابی دیتابیس", "نگهداری"),
        new("AUDIT", "مشاهده لاگ ممیزی", "نگهداری")
    };

    public static AdminPermissionItem? Find(string code) =>
        All.FirstOrDefault(
            x => string.Equals(
                x.Code,
                code,
                StringComparison.Ordinal
            )
        );

    public static bool Exists(string code) =>
        Find(code) != null;
}

static partial class Database
{
    public static void InitAdminPanel(long ownerId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS AdminUsers(
                AdminId INTEGER PRIMARY KEY,
                DisplayName TEXT NOT NULL DEFAULT '',
                AddedBy INTEGER NOT NULL DEFAULT 0,
                IsOwner INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAtMs INTEGER NOT NULL,
                LastSeenMs INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS AdminPermissions(
                AdminId INTEGER NOT NULL,
                PermissionCode TEXT NOT NULL,
                GrantedBy INTEGER NOT NULL,
                GrantedAtMs INTEGER NOT NULL,
                PRIMARY KEY(AdminId, PermissionCode)
            );

            CREATE INDEX IF NOT EXISTS IX_AdminPermissions_AdminId
                ON AdminPermissions(AdminId);

            CREATE TABLE IF NOT EXISTS AdminAudit(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AdminId INTEGER NOT NULL,
                Action TEXT NOT NULL,
                TargetType TEXT NOT NULL DEFAULT '',
                TargetId TEXT NOT NULL DEFAULT '',
                Details TEXT NOT NULL DEFAULT '',
                Success INTEGER NOT NULL DEFAULT 1,
                CreatedAtMs INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_AdminAudit_CreatedAtMs
                ON AdminAudit(CreatedAtMs DESC);

            CREATE INDEX IF NOT EXISTS IX_AdminAudit_AdminId
                ON AdminAudit(AdminId, CreatedAtMs DESC);

            CREATE TABLE IF NOT EXISTS AdminPendingActions(
                ActionId TEXT PRIMARY KEY,
                AdminId INTEGER NOT NULL,
                ActionType TEXT NOT NULL,
                TargetType TEXT NOT NULL DEFAULT '',
                TargetId TEXT NOT NULL DEFAULT '',
                Payload TEXT NOT NULL DEFAULT '',
                Stage INTEGER NOT NULL DEFAULT 1,
                ExpiresAtMs INTEGER NOT NULL,
                CreatedAtMs INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_AdminPendingActions_Expires
                ON AdminPendingActions(ExpiresAtMs);
        ";

        cmd.ExecuteNonQuery();

        long nowMs =
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        using var ownerCmd = con.CreateCommand();

        ownerCmd.CommandText = @"
            INSERT INTO AdminUsers(
                AdminId,
                DisplayName,
                AddedBy,
                IsOwner,
                IsActive,
                CreatedAtMs,
                LastSeenMs
            )
            VALUES(
                $adminId,
                'مالک اصلی آلیس',
                $adminId,
                1,
                1,
                $nowMs,
                $nowMs
            )
            ON CONFLICT(AdminId) DO UPDATE SET
                IsOwner = 1,
                IsActive = 1,
                DisplayName =
                    CASE
                        WHEN AdminUsers.DisplayName = ''
                        THEN 'مالک اصلی آلیس'
                        ELSE AdminUsers.DisplayName
                    END;
        ";

        ownerCmd.Parameters.AddWithValue(
            "$adminId",
            ownerId
        );

        ownerCmd.Parameters.AddWithValue(
            "$nowMs",
            nowMs
        );

        ownerCmd.ExecuteNonQuery();

        CleanupExpiredAdminActions();
    }

    public static bool IsAdminActive(long adminId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            SELECT COUNT(*)
            FROM AdminUsers
            WHERE AdminId = $adminId
              AND IsActive = 1;
        ";

        cmd.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        return Convert.ToInt64(
            cmd.ExecuteScalar()
        ) > 0;
    }

    public static AdminAccount? GetAdmin(long adminId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            SELECT
                AdminId,
                DisplayName,
                AddedBy,
                IsOwner,
                IsActive,
                CreatedAtMs,
                LastSeenMs
            FROM AdminUsers
            WHERE AdminId = $adminId;
        ";

        cmd.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return ReadAdminAccount(reader);
    }

    public static List<AdminAccount> GetAdmins()
    {
        var result = new List<AdminAccount>();

        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            SELECT
                AdminId,
                DisplayName,
                AddedBy,
                IsOwner,
                IsActive,
                CreatedAtMs,
                LastSeenMs
            FROM AdminUsers
            ORDER BY
                IsOwner DESC,
                IsActive DESC,
                CreatedAtMs DESC;
        ";

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
            result.Add(ReadAdminAccount(reader));

        return result;
    }

    private static AdminAccount ReadAdminAccount(
        SqliteDataReader reader)
    {
        return new AdminAccount
        {
            AdminId = reader.GetInt64(0),
            DisplayName = reader.GetString(1),
            AddedBy = reader.GetInt64(2),
            IsOwner = reader.GetInt64(3) != 0,
            IsActive = reader.GetInt64(4) != 0,
            CreatedAtMs = reader.GetInt64(5),
            LastSeenMs = reader.GetInt64(6)
        };
    }

    public static void AddOrReactivateAdmin(
        long adminId,
        string displayName,
        long addedBy)
    {
        long nowMs =
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO AdminUsers(
                AdminId,
                DisplayName,
                AddedBy,
                IsOwner,
                IsActive,
                CreatedAtMs,
                LastSeenMs
            )
            VALUES(
                $adminId,
                $displayName,
                $addedBy,
                0,
                1,
                $nowMs,
                0
            )
            ON CONFLICT(AdminId) DO UPDATE SET
                DisplayName = excluded.DisplayName,
                AddedBy = excluded.AddedBy,
                IsActive = 1;
        ";

        cmd.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        cmd.Parameters.AddWithValue(
            "$displayName",
            displayName
        );

        cmd.Parameters.AddWithValue(
            "$addedBy",
            addedBy
        );

        cmd.Parameters.AddWithValue(
            "$nowMs",
            nowMs
        );

        cmd.ExecuteNonQuery();
    }

    public static void SetAdminActive(
        long adminId,
        bool active)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            UPDATE AdminUsers
            SET IsActive = $active
            WHERE AdminId = $adminId
              AND IsOwner = 0;
        ";

        cmd.Parameters.AddWithValue(
            "$active",
            active ? 1 : 0
        );

        cmd.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        cmd.ExecuteNonQuery();
    }

    public static bool DeleteAdmin(long adminId)
    {
        using var con = OpenCon();
        using var transaction = con.BeginTransaction();

        using var check = con.CreateCommand();
        check.Transaction = transaction;

        check.CommandText = @"
            SELECT IsOwner
            FROM AdminUsers
            WHERE AdminId = $adminId;
        ";

        check.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        object? value = check.ExecuteScalar();

        if (value == null ||
            value == DBNull.Value ||
            Convert.ToInt64(value) != 0)
        {
            transaction.Rollback();
            return false;
        }

        using var permissions = con.CreateCommand();
        permissions.Transaction = transaction;

        permissions.CommandText = @"
            DELETE FROM AdminPermissions
            WHERE AdminId = $adminId;
        ";

        permissions.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        permissions.ExecuteNonQuery();

        using var admin = con.CreateCommand();
        admin.Transaction = transaction;

        admin.CommandText = @"
            DELETE FROM AdminUsers
            WHERE AdminId = $adminId
              AND IsOwner = 0;
        ";

        admin.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        int affected = admin.ExecuteNonQuery();

        transaction.Commit();
        return affected > 0;
    }

    public static void TouchAdmin(long adminId)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            UPDATE AdminUsers
            SET LastSeenMs = $nowMs
            WHERE AdminId = $adminId;
        ";

        cmd.Parameters.AddWithValue(
            "$nowMs",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );

        cmd.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        cmd.ExecuteNonQuery();
    }

    public static HashSet<string> GetAdminPermissions(
        long adminId)
    {
        var result =
            new HashSet<string>(
                StringComparer.Ordinal
            );

        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            SELECT PermissionCode
            FROM AdminPermissions
            WHERE AdminId = $adminId;
        ";

        cmd.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
            result.Add(reader.GetString(0));

        return result;
    }

    public static bool HasAdminPermission(
        long adminId,
        string permissionCode)
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            SELECT COUNT(*)
            FROM AdminPermissions
            WHERE AdminId = $adminId
              AND PermissionCode = $permissionCode;
        ";

        cmd.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        cmd.Parameters.AddWithValue(
            "$permissionCode",
            permissionCode
        );

        return Convert.ToInt64(
            cmd.ExecuteScalar()
        ) > 0;
    }

    public static void SetAdminPermission(
        long adminId,
        string permissionCode,
        bool enabled,
        long grantedBy)
    {
        if (!AdminPermissionCatalog.Exists(permissionCode))
            throw new ArgumentException(
                "Unknown permission.",
                nameof(permissionCode)
            );

        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        if (enabled)
        {
            cmd.CommandText = @"
                INSERT INTO AdminPermissions(
                    AdminId,
                    PermissionCode,
                    GrantedBy,
                    GrantedAtMs
                )
                VALUES(
                    $adminId,
                    $permissionCode,
                    $grantedBy,
                    $nowMs
                )
                ON CONFLICT(
                    AdminId,
                    PermissionCode
                ) DO UPDATE SET
                    GrantedBy = excluded.GrantedBy,
                    GrantedAtMs = excluded.GrantedAtMs;
            ";

            cmd.Parameters.AddWithValue(
                "$grantedBy",
                grantedBy
            );

            cmd.Parameters.AddWithValue(
                "$nowMs",
                DateTimeOffset.UtcNow
                    .ToUnixTimeMilliseconds()
            );
        }
        else
        {
            cmd.CommandText = @"
                DELETE FROM AdminPermissions
                WHERE AdminId = $adminId
                  AND PermissionCode = $permissionCode;
            ";
        }

        cmd.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        cmd.Parameters.AddWithValue(
            "$permissionCode",
            permissionCode
        );

        cmd.ExecuteNonQuery();
    }

    public static void SetAllAdminPermissions(
        long adminId,
        bool enabled,
        long grantedBy)
    {
        using var con = OpenCon();
        using var transaction = con.BeginTransaction();

        using var clear = con.CreateCommand();
        clear.Transaction = transaction;

        clear.CommandText = @"
            DELETE FROM AdminPermissions
            WHERE AdminId = $adminId;
        ";

        clear.Parameters.AddWithValue(
            "$adminId",
            adminId
        );

        clear.ExecuteNonQuery();

        if (enabled)
        {
            long nowMs =
                DateTimeOffset.UtcNow
                    .ToUnixTimeMilliseconds();

            foreach (var permission in
                     AdminPermissionCatalog.All)
            {
                using var insert = con.CreateCommand();
                insert.Transaction = transaction;

                insert.CommandText = @"
                    INSERT INTO AdminPermissions(
                        AdminId,
                        PermissionCode,
                        GrantedBy,
                        GrantedAtMs
                    )
                    VALUES(
                        $adminId,
                        $code,
                        $grantedBy,
                        $nowMs
                    );
                ";

                insert.Parameters.AddWithValue(
                    "$adminId",
                    adminId
                );

                insert.Parameters.AddWithValue(
                    "$code",
                    permission.Code
                );

                insert.Parameters.AddWithValue(
                    "$grantedBy",
                    grantedBy
                );

                insert.Parameters.AddWithValue(
                    "$nowMs",
                    nowMs
                );

                insert.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    public static void WriteAdminAudit(
        long adminId,
        string action,
        string targetType = "",
        string targetId = "",
        string details = "",
        bool success = true)
    {
        try
        {
            using var con = OpenCon();
            using var cmd = con.CreateCommand();

            cmd.CommandText = @"
                INSERT INTO AdminAudit(
                    AdminId,
                    Action,
                    TargetType,
                    TargetId,
                    Details,
                    Success,
                    CreatedAtMs
                )
                VALUES(
                    $adminId,
                    $action,
                    $targetType,
                    $targetId,
                    $details,
                    $success,
                    $nowMs
                );
            ";

            cmd.Parameters.AddWithValue(
                "$adminId",
                adminId
            );

            cmd.Parameters.AddWithValue(
                "$action",
                action
            );

            cmd.Parameters.AddWithValue(
                "$targetType",
                targetType
            );

            cmd.Parameters.AddWithValue(
                "$targetId",
                targetId
            );

            cmd.Parameters.AddWithValue(
                "$details",
                details
            );

            cmd.Parameters.AddWithValue(
                "$success",
                success ? 1 : 0
            );

            cmd.Parameters.AddWithValue(
                "$nowMs",
                DateTimeOffset.UtcNow
                    .ToUnixTimeMilliseconds()
            );

            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[ADMIN AUDIT ERR] {ex.Message}"
            );
        }
    }

    public static List<AdminAuditEntry> GetRecentAdminAudit(
        int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 100);

        var result = new List<AdminAuditEntry>();

        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            SELECT
                Id,
                AdminId,
                Action,
                TargetType,
                TargetId,
                Details,
                Success,
                CreatedAtMs
            FROM AdminAudit
            ORDER BY CreatedAtMs DESC
            LIMIT $limit;
        ";

        cmd.Parameters.AddWithValue(
            "$limit",
            limit
        );

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            result.Add(new AdminAuditEntry
            {
                Id = reader.GetInt64(0),
                AdminId = reader.GetInt64(1),
                Action = reader.GetString(2),
                TargetType = reader.GetString(3),
                TargetId = reader.GetString(4),
                Details = reader.GetString(5),
                Success = reader.GetInt64(6) != 0,
                CreatedAtMs = reader.GetInt64(7)
            });
        }

        return result;
    }

    public static AdminDashboardStats GetAdminDashboardStats()
    {
        long nowMs =
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            SELECT
                (SELECT COUNT(*) FROM Countries),
                (
                    SELECT COUNT(DISTINCT OwnerId)
                    FROM Countries
                ),
                (
                    SELECT COUNT(DISTINCT ChatId)
                    FROM Countries
                ),
                (SELECT COUNT(*) FROM Alliances),
                (
                    SELECT COUNT(*)
                    FROM Transfers
                    WHERE ArriveAtMs > $nowMs
                ),
                (
                    SELECT COUNT(*)
                    FROM Deployments
                    WHERE EndAtMs > $nowMs
                ),
                (
                    SELECT COUNT(*)
                    FROM AdminUsers
                    WHERE IsActive = 1
                ),
                (SELECT COUNT(*) FROM AdminAudit);
        ";

        cmd.Parameters.AddWithValue(
            "$nowMs",
            nowMs
        );

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return new AdminDashboardStats();

        return new AdminDashboardStats
        {
            Countries =
                Convert.ToInt32(reader.GetInt64(0)),
            Players =
                Convert.ToInt32(reader.GetInt64(1)),
            Groups =
                Convert.ToInt32(reader.GetInt64(2)),
            Alliances =
                Convert.ToInt32(reader.GetInt64(3)),
            ActiveTransfers =
                Convert.ToInt32(reader.GetInt64(4)),
            ActiveDeployments =
                Convert.ToInt32(reader.GetInt64(5)),
            ActiveAdmins =
                Convert.ToInt32(reader.GetInt64(6)),
            AuditEntries =
                Convert.ToInt32(reader.GetInt64(7))
        };
    }

    public static void CleanupExpiredAdminActions()
    {
        try
        {
            using var con = OpenCon();
            using var cmd = con.CreateCommand();

            cmd.CommandText = @"
                DELETE FROM AdminPendingActions
                WHERE ExpiresAtMs <= $nowMs;
            ";

            cmd.Parameters.AddWithValue(
                "$nowMs",
                DateTimeOffset.UtcNow
                    .ToUnixTimeMilliseconds()
            );

            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[ADMIN ACTION CLEANUP ERR] {ex.Message}"
            );
        }
    }
}

// ===== MERGED ADMIN PANEL =====
sealed class AdminInputRequest
{
    public string Kind { get; set; } = "";
    public long ExpiresAtMs { get; set; }
}

partial class Program
{
    static readonly ConcurrentDictionary<long, AdminInputRequest>
        adminInputRequests = new();

    static bool IsPanelOwner(long userId) =>
        userId == OWNER_ID;

    static bool IsPanelAdmin(long userId) =>
        userId == OWNER_ID ||
        Database.IsAdminActive(userId);

    static bool CanAdmin(long userId, string permissionCode) =>
        userId == OWNER_ID ||
        Database.HasAdminPermission(
            userId,
            permissionCode
        );

    static bool CanAdminAny(
        long userId,
        params string[] permissionCodes)
    {
        if (userId == OWNER_ID)
            return true;

        foreach (string code in permissionCodes)
        {
            if (Database.HasAdminPermission(userId, code))
                return true;
        }

        return false;
    }

    static KeyboardButton AdminKeyboardButton(string text) =>
        new(text);

    static ReplyKeyboardMarkup BuildAdminReplyKeyboard(long userId)
    {
        var rows = new List<KeyboardButton[]>
        {
            new[]
            {
                AdminKeyboardButton("🧭 پنل مدیریت"),
                AdminKeyboardButton("📊 داشبورد")
            }
        };

        if (IsPanelOwner(userId))
        {
            rows.Add(new[]
            {
                AdminKeyboardButton("👮 مدیریت ادمین‌ها"),
                AdminKeyboardButton("📜 لاگ مدیریتی")
            });
        }
        else if (CanAdmin(userId, "AUDIT"))
        {
            rows.Add(new[]
            {
                AdminKeyboardButton("📜 لاگ مدیریتی")
            });
        }

        if (CanAdminAny(userId, "P_VIEW", "C_VIEW"))
        {
            rows.Add(new[]
            {
                AdminKeyboardButton("🔎 جستجوی پلیر"),
                AdminKeyboardButton("🌍 مدیریت کشور")
            });
        }

        if (CanAdminAny(userId, "G_VIEW", "G_EDIT", "ALLY"))
        {
            rows.Add(new[]
            {
                AdminKeyboardButton("👥 مدیریت گروه"),
                AdminKeyboardButton("🤝 مدیریت اتحاد")
            });
        }

        if (CanAdminAny(userId, "ROYAL", "E_GLOBAL", "ANN"))
        {
            rows.Add(new[]
            {
                AdminKeyboardButton("💎 اقتصاد و رویال"),
                AdminKeyboardButton("📢 اعلامیه")
            });
        }

        if (CanAdminAny(userId, "W_VIEW", "W_EDIT", "O_VIEW", "O_EDIT"))
        {
            rows.Add(new[]
            {
                AdminKeyboardButton("⚔️ جنگ و عملیات")
            });
        }

        if (CanAdminAny(userId, "SET", "BACKUP", "RESTORE"))
        {
            rows.Add(new[]
            {
                AdminKeyboardButton("🗄 نگهداری"),
                AdminKeyboardButton("⚙️ تنظیمات")
            });
        }

        rows.Add(new[]
        {
            AdminKeyboardButton("❌ بستن پنل")
        });

        return new ReplyKeyboardMarkup(rows)
        {
            ResizeKeyboard = true
        };
    }

    static async Task<bool> TryHandleAdminPrivateMessageAsync(
        Message message,
        User user,
        CancellationToken ct)
    {
        long userId = user.Id;

        if (!IsPanelAdmin(userId))
            return false;

        Database.TouchAdmin(userId);

        string text = message.Text?.Trim() ?? "";

        if (adminInputRequests.TryGetValue(
                userId,
                out var request))
        {
            long nowMs =
                DateTimeOffset.UtcNow
                    .ToUnixTimeMilliseconds();

            if (request.ExpiresAtMs <= nowMs)
            {
                adminInputRequests.TryRemove(
                    userId,
                    out _
                );

                await SendPermanent(
                    userId,
                    "⌛ زمان عملیات مدیریتی تمام شد.",
                    ct: ct
                );
            }
            else if (request.Kind == "add_admin")
            {
                await HandleAdminAddInput(
                    userId,
                    text,
                    ct
                );

                return true;
            }
        }

        switch (text)
        {
            case "پنل":
            case "/panel":
            case "/admin":
            case "🧭 پنل مدیریت":
                await SendAdminHome(userId, ct);
                return true;

            case "📊 داشبورد":
                await SendAdminDashboard(userId, ct);
                return true;

            case "👮 مدیریت ادمین‌ها":
                if (!IsPanelOwner(userId))
                {
                    await SendAdminDenied(userId, ct);
                    return true;
                }

                await SendAdminList(userId, 0, ct);
                return true;

            case "📜 لاگ مدیریتی":
                await SendAdminAudit(userId, ct);
                return true;

            case "🔎 جستجوی پلیر":
                await SendAdminModulePending(
                    userId,
                    "جستجوی پلیر",
                    "P_VIEW",
                    ct
                );
                return true;

            case "🌍 مدیریت کشور":
                await SendAdminModulePending(
                    userId,
                    "مدیریت کشور",
                    "C_VIEW",
                    ct
                );
                return true;

            case "👥 مدیریت گروه":
                await SendAdminModulePending(
                    userId,
                    "مدیریت گروه",
                    "G_VIEW",
                    ct
                );
                return true;

            case "🤝 مدیریت اتحاد":
                await SendAdminModulePending(
                    userId,
                    "مدیریت اتحاد",
                    "ALLY",
                    ct
                );
                return true;

            case "💎 اقتصاد و رویال":
                await SendAdminModulePending(
                    userId,
                    "اقتصاد و رویال",
                    "ROYAL",
                    ct
                );
                return true;

            case "📢 اعلامیه":
                await SendAdminModulePending(
                    userId,
                    "اعلامیه",
                    "ANN",
                    ct
                );
                return true;

            case "⚔️ جنگ و عملیات":
                await SendAdminModulePending(
                    userId,
                    "جنگ و عملیات",
                    "W_VIEW",
                    ct
                );
                return true;

            case "🗄 نگهداری":
                await SendAdminModulePending(
                    userId,
                    "نگهداری",
                    "BACKUP",
                    ct
                );
                return true;

            case "⚙️ تنظیمات":
                await SendAdminModulePending(
                    userId,
                    "تنظیمات",
                    "SET",
                    ct
                );
                return true;

            case "❌ بستن پنل":
                adminInputRequests.TryRemove(
                    userId,
                    out _
                );

                await SendPermanent(
                    userId,
                    "✅ پنل مدیریت بسته شد.",
                    markup: new ReplyKeyboardRemove(),
                    ct: ct
                );

                return true;
        }

        return false;
    }

    static async Task HandleAdminAddInput(
        long ownerId,
        string text,
        CancellationToken ct)
    {
        if (!IsPanelOwner(ownerId))
        {
            adminInputRequests.TryRemove(
                ownerId,
                out _
            );

            await SendAdminDenied(ownerId, ct);
            return;
        }

        if (text is "لغو" or "cancel" or "انصراف")
        {
            adminInputRequests.TryRemove(
                ownerId,
                out _
            );

            await SendAdminHome(ownerId, ct);
            return;
        }

        string[] values = text.Split(
            ' ',
            2,
            StringSplitOptions.RemoveEmptyEntries
        );

        if (values.Length == 0 ||
            !TryParseLong(
                NormalizeDigits(values[0]),
                out long targetId
            ) ||
            targetId <= 0)
        {
            await SendPermanent(
                ownerId,
                "❌ فرمت نامعتبر است.\n\n" +
                "فرمت صحیح:\n" +
                "123456789 نام مدیر\n\n" +
                "برای لغو بنویسید: لغو",
                ct: ct
            );
            return;
        }

        if (targetId == OWNER_ID)
        {
            await SendPermanent(
                ownerId,
                "ℹ️ این آیدی متعلق به مالک اصلی است.",
                ct: ct
            );
            return;
        }

        string displayName =
            values.Length >= 2
                ? values[1].Trim()
                : $"مدیر {targetId}";

        if (displayName.Length > 80)
            displayName = displayName[..80];

        Database.AddOrReactivateAdmin(
            targetId,
            displayName,
            ownerId
        );

        Database.WriteAdminAudit(
            ownerId,
            "ADMIN_ADD",
            "Admin",
            targetId.ToString(),
            $"Name={displayName}"
        );

        adminInputRequests.TryRemove(
            ownerId,
            out _
        );

        await SendPermanent(
            ownerId,
            $"✅ مدیر اضافه شد.\n" +
            $"نام: {displayName}\n" +
            $"آیدی: {targetId}\n\n" +
            "این مدیر هنوز هیچ دسترسی عملیاتی ندارد.",
            ct: ct
        );

        try
        {
            await SendPermanent(
                targetId,
                "👮 شما به‌عنوان مدیر آلیس ثبت شدید.\n" +
                "دسترسی‌های شما توسط مالک تعیین می‌شود.",
                markup: BuildAdminReplyKeyboard(targetId),
                ct: ct
            );
        }
        catch
        {
        }

        await SendAdminDetails(
            ownerId,
            targetId,
            ct
        );
    }

    static async Task SendAdminHome(
        long userId,
        CancellationToken ct)
    {
        var screen = BuildAdminHomeScreen(userId);

        await SendPermanent(
            userId,
            "⌨️ میانبرهای مدیریت آلیس فعال شدند.",
            markup: BuildAdminReplyKeyboard(userId),
            ct: ct
        );

        await SendPermanent(
            userId,
            screen.Text,
            markup: screen.Keyboard,
            ct: ct
        );
    }

    static (
        string Text,
        InlineKeyboardMarkup Keyboard
    ) BuildAdminHomeScreen(long userId)
    {
        var account = Database.GetAdmin(userId);

        string displayName =
            account?.DisplayName ??
            (userId == OWNER_ID
                ? "مالک اصلی"
                : $"مدیر {userId}");

        var buttons =
            new List<InlineKeyboardButton>();

        if (CanAdmin(userId, "DASH"))
        {
            buttons.Add(
                InlineKeyboardButton.WithCallbackData(
                    "📊 داشبورد",
                    "adm:dash"
                )
            );
        }

        if (IsPanelOwner(userId))
        {
            buttons.Add(
                InlineKeyboardButton.WithCallbackData(
                    "👮 مدیران",
                    "adm:admins:0"
                )
            );
        }

        AddAdminModuleButton(
            buttons,
            userId,
            "🔎 پلیرها",
            "players",
            "P_VIEW"
        );

        AddAdminModuleButton(
            buttons,
            userId,
            "🌍 کشورها",
            "countries",
            "C_VIEW"
        );

        AddAdminModuleButton(
            buttons,
            userId,
            "👥 گروه‌ها",
            "groups",
            "G_VIEW"
        );

        AddAdminModuleButton(
            buttons,
            userId,
            "🤝 اتحادها",
            "alliances",
            "ALLY"
        );

        AddAdminModuleButton(
            buttons,
            userId,
            "💎 اقتصاد",
            "economy",
            "ROYAL"
        );

        AddAdminModuleButton(
            buttons,
            userId,
            "⚔️ جنگ",
            "war",
            "W_VIEW"
        );

        AddAdminModuleButton(
            buttons,
            userId,
            "🚚 عملیات",
            "operations",
            "O_VIEW"
        );

        AddAdminModuleButton(
            buttons,
            userId,
            "📢 اعلامیه",
            "announce",
            "ANN"
        );

        AddAdminModuleButton(
            buttons,
            userId,
            "⚙️ تنظیمات",
            "settings",
            "SET"
        );

        if (CanAdminAny(
                userId,
                "BACKUP",
                "RESTORE"))
        {
            buttons.Add(
                InlineKeyboardButton.WithCallbackData(
                    "🗄 نگهداری",
                    "adm:todo:maintenance"
                )
            );
        }

        if (CanAdmin(userId, "AUDIT"))
        {
            buttons.Add(
                InlineKeyboardButton.WithCallbackData(
                    "📜 Audit Log",
                    "adm:audit"
                )
            );
        }

        var rows = PairAdminButtons(buttons);

        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "🔄 تازه‌سازی",
                "adm:home"
            ),
            InlineKeyboardButton.WithCallbackData(
                "❌ بستن",
                "adm:close"
            )
        });

        string text =
            "🧭 پنل مدیریت آلیس\n\n" +
            $"مدیر: {displayName}\n" +
            $"آیدی: {userId}\n" +
            $"سطح: {(userId == OWNER_ID ? "مالک اصلی" : "مدیر")}\n\n" +
            "یک بخش را انتخاب کنید.";

        return (
            text,
            new InlineKeyboardMarkup(rows)
        );
    }

    static void AddAdminModuleButton(
        List<InlineKeyboardButton> buttons,
        long userId,
        string title,
        string module,
        string permission)
    {
        if (!CanAdmin(userId, permission))
            return;

        buttons.Add(
            InlineKeyboardButton.WithCallbackData(
                title,
                $"adm:todo:{module}"
            )
        );
    }

    static List<InlineKeyboardButton[]> PairAdminButtons(
        List<InlineKeyboardButton> buttons)
    {
        var rows =
            new List<InlineKeyboardButton[]>();

        for (int i = 0; i < buttons.Count; i += 2)
        {
            if (i + 1 < buttons.Count)
            {
                rows.Add(new[]
                {
                    buttons[i],
                    buttons[i + 1]
                });
            }
            else
            {
                rows.Add(new[]
                {
                    buttons[i]
                });
            }
        }

        return rows;
    }

    static async Task SendAdminDashboard(
        long userId,
        CancellationToken ct)
    {
        if (!CanAdmin(userId, "DASH"))
        {
            await SendAdminDenied(userId, ct);
            return;
        }

        var screen = BuildAdminDashboardScreen();

        await SendPermanent(
            userId,
            screen.Text,
            markup: screen.Keyboard,
            ct: ct
        );
    }

    static (
        string Text,
        InlineKeyboardMarkup Keyboard
    ) BuildAdminDashboardScreen()
    {
        var stats =
            Database.GetAdminDashboardStats();

        var activity =
            Database.GetActivityStats();

        long databaseSize = 0;

        try
        {
            if (System.IO.File.Exists("gamedata.db"))
                databaseSize =
                    new System.IO.FileInfo("gamedata.db").Length;
        }
        catch
        {
        }

        string updateSchedule =
            UpdateMode == "daily"
                ? $"روزانه در {UpdateValue / 60:D2}:{UpdateValue % 60:D2}"
                : $"هر {UpdateValue} دقیقه";

        string lastUpdate =
            lastAssetUpdateAt == DateTime.MinValue
                ? "هنوز اجرا نشده"
                : lastAssetUpdateAt
                    .AddHours(3.5)
                    .ToString("yyyy-MM-dd HH:mm");

        string text =
            "📊 داشبورد مدیریتی آلیس\n\n" +

            "🌍 وضعیت بازی\n" +
            $"کشورها: {stats.Countries:N0}\n" +
            $"پلیرهای ثبت‌شده: {stats.Players:N0}\n" +
            $"گروه‌های دارای کشور: {stats.Groups:N0}\n" +
            $"اتحادها: {stats.Alliances:N0}\n\n" +

            "🟢 فعالیت\n" +
            $"۲۴ ساعت: {activity.Players24h:N0} پلیر | " +
            $"{activity.Groups24h:N0} گپ\n" +
            $"۷ روز: {activity.Players7d:N0} پلیر | " +
            $"{activity.Groups7d:N0} گپ\n" +
            $"۳۰ روز: {activity.Players30d:N0} پلیر | " +
            $"{activity.Groups30d:N0} گپ\n\n" +

            "🚚 عملیات فعال\n" +
            $"ترنسفر: {stats.ActiveTransfers:N0}\n" +
            $"صف‌آرایی: {stats.ActiveDeployments:N0}\n\n" +

            "⚙️ سیستم\n" +
            $"ادمین فعال: {stats.ActiveAdmins:N0}\n" +
            $"Audit: {stats.AuditEntries:N0}\n" +
            $"حجم دیتابیس: {databaseSize / 1024.0 / 1024.0:F2} MB\n" +
            $"آپدیت دارایی: {updateSchedule}\n" +
            $"آخرین آپدیت: {lastUpdate}";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🔄 تازه‌سازی",
                    "adm:dash"
                )
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🏠 خانه",
                    "adm:home"
                )
            }
        });

        return (text, keyboard);
    }

    static async Task SendAdminList(
        long ownerId,
        int page,
        CancellationToken ct)
    {
        if (!IsPanelOwner(ownerId))
        {
            await SendAdminDenied(ownerId, ct);
            return;
        }

        var screen = BuildAdminListScreen(page);

        await SendPermanent(
            ownerId,
            screen.Text,
            markup: screen.Keyboard,
            ct: ct
        );
    }

    static (
        string Text,
        InlineKeyboardMarkup Keyboard
    ) BuildAdminListScreen(int requestedPage)
    {
        var admins = Database.GetAdmins();

        const int pageSize = 6;

        int totalPages = Math.Max(
            1,
            (int)Math.Ceiling(
                admins.Count / (double)pageSize
            )
        );

        int page = Math.Clamp(
            requestedPage,
            0,
            totalPages - 1
        );

        var items = admins
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToList();

        var rows =
            new List<InlineKeyboardButton[]>();

        foreach (var admin in items)
        {
            string icon = admin.IsOwner
                ? "👑"
                : admin.IsActive
                    ? "🟢"
                    : "🔴";

            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{icon} {admin.DisplayName}",
                    $"adm:view:{admin.AdminId}"
                )
            });
        }

        var navigation =
            new List<InlineKeyboardButton>();

        if (page > 0)
        {
            navigation.Add(
                InlineKeyboardButton.WithCallbackData(
                    "⬅️ قبلی",
                    $"adm:admins:{page - 1}"
                )
            );
        }

        if (page + 1 < totalPages)
        {
            navigation.Add(
                InlineKeyboardButton.WithCallbackData(
                    "بعدی ➡️",
                    $"adm:admins:{page + 1}"
                )
            );
        }

        if (navigation.Count > 0)
            rows.Add(navigation.ToArray());

        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "➕ افزودن مدیر",
                "adm:add"
            )
        });

        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "🏠 خانه",
                "adm:home"
            )
        });

        string text =
            "👮 مدیریت ادمین‌های آلیس\n\n" +
            $"تعداد: {admins.Count:N0}\n" +
            $"صفحه: {page + 1}/{totalPages}\n\n" +
            "👑 مالک | 🟢 فعال | 🔴 غیرفعال";

        return (
            text,
            new InlineKeyboardMarkup(rows)
        );
    }

    static async Task SendAdminDetails(
        long ownerId,
        long targetId,
        CancellationToken ct)
    {
        if (!IsPanelOwner(ownerId))
        {
            await SendAdminDenied(ownerId, ct);
            return;
        }

        var screen =
            BuildAdminDetailsScreen(targetId);

        await SendPermanent(
            ownerId,
            screen.Text,
            markup: screen.Keyboard,
            ct: ct
        );
    }

    static (
        string Text,
        InlineKeyboardMarkup Keyboard
    ) BuildAdminDetailsScreen(long targetId)
    {
        var admin = Database.GetAdmin(targetId);

        if (admin == null)
        {
            return (
                "❌ مدیر یافت نشد.",
                new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "⬅️ بازگشت",
                            "adm:admins:0"
                        )
                    }
                })
            );
        }

        var permissions =
            Database.GetAdminPermissions(targetId);

        string created =
            FormatAdminTime(admin.CreatedAtMs);

        string lastSeen =
            admin.LastSeenMs > 0
                ? FormatAdminTime(admin.LastSeenMs)
                : "بدون فعالیت";

        var rows =
            new List<InlineKeyboardButton[]>();

        if (!admin.IsOwner)
        {
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"🔐 دسترسی‌ها ({permissions.Count})",
                    $"adm:perms:{targetId}:0"
                )
            });

            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    admin.IsActive
                        ? "⏸ غیرفعال‌کردن"
                        : "▶️ فعال‌کردن",
                    $"adm:toggle:{targetId}"
                )
            });

            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🗑 حذف مدیر",
                    $"adm:removeask:{targetId}"
                )
            });
        }

        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "⬅️ فهرست مدیران",
                "adm:admins:0"
            ),
            InlineKeyboardButton.WithCallbackData(
                "🏠 خانه",
                "adm:home"
            )
        });

        string text =
            "👮 مشخصات مدیر\n\n" +
            $"نام: {admin.DisplayName}\n" +
            $"آیدی: {admin.AdminId}\n" +
            $"نوع: {(admin.IsOwner ? "مالک اصلی" : "مدیر")}\n" +
            $"وضعیت: {(admin.IsActive ? "فعال 🟢" : "غیرفعال 🔴")}\n" +
            $"تعداد دسترسی: {permissions.Count}\n" +
            $"افزوده‌شده توسط: {admin.AddedBy}\n" +
            $"تاریخ افزودن: {created}\n" +
            $"آخرین فعالیت: {lastSeen}";

        return (
            text,
            new InlineKeyboardMarkup(rows)
        );
    }

    static (
        string Text,
        InlineKeyboardMarkup Keyboard
    ) BuildAdminPermissionScreen(
        long targetId,
        int requestedPage)
    {
        var admin = Database.GetAdmin(targetId);

        if (admin == null || admin.IsOwner)
        {
            return (
                "❌ مدیر قابل ویرایش نیست.",
                new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "⬅️ بازگشت",
                            "adm:admins:0"
                        )
                    }
                })
            );
        }

        var granted =
            Database.GetAdminPermissions(targetId);

        const int pageSize = 7;

        int totalPages = Math.Max(
            1,
            (int)Math.Ceiling(
                AdminPermissionCatalog.All.Length /
                (double)pageSize
            )
        );

        int page = Math.Clamp(
            requestedPage,
            0,
            totalPages - 1
        );

        var permissions =
            AdminPermissionCatalog.All
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

        var rows =
            new List<InlineKeyboardButton[]>();

        foreach (var permission in permissions)
        {
            bool enabled =
                granted.Contains(permission.Code);

            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{(enabled ? "✅" : "▫️")} " +
                    $"{permission.Title}",
                    $"adm:perm:{targetId}:" +
                    $"{permission.Code}:{page}"
                )
            });
        }

        var navigation =
            new List<InlineKeyboardButton>();

        if (page > 0)
        {
            navigation.Add(
                InlineKeyboardButton.WithCallbackData(
                    "⬅️ قبلی",
                    $"adm:perms:{targetId}:{page - 1}"
                )
            );
        }

        if (page + 1 < totalPages)
        {
            navigation.Add(
                InlineKeyboardButton.WithCallbackData(
                    "بعدی ➡️",
                    $"adm:perms:{targetId}:{page + 1}"
                )
            );
        }

        if (navigation.Count > 0)
            rows.Add(navigation.ToArray());

        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "✅ اعطای همه",
                $"adm:allask:{targetId}:1"
            ),
            InlineKeyboardButton.WithCallbackData(
                "🚫 حذف همه",
                $"adm:allask:{targetId}:0"
            )
        });

        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "⬅️ مشخصات مدیر",
                $"adm:view:{targetId}"
            )
        });

        string text =
            "🔐 مدیریت دسترسی‌ها\n\n" +
            $"مدیر: {admin.DisplayName}\n" +
            $"آیدی: {targetId}\n" +
            $"دسترسی فعال: {granted.Count}\n" +
            $"صفحه: {page + 1}/{totalPages}\n\n" +
            "برای تغییر هر دسترسی روی آن بزنید.";

        return (
            text,
            new InlineKeyboardMarkup(rows)
        );
    }

    static async Task SendAdminAudit(
        long userId,
        CancellationToken ct)
    {
        if (!CanAdmin(userId, "AUDIT"))
        {
            await SendAdminDenied(userId, ct);
            return;
        }

        var screen = BuildAdminAuditScreen();

        await SendPermanent(
            userId,
            screen.Text,
            markup: screen.Keyboard,
            ct: ct
        );
    }

    static (
        string Text,
        InlineKeyboardMarkup Keyboard
    ) BuildAdminAuditScreen()
    {
        var entries =
            Database.GetRecentAdminAudit(15);

        var text = new StringBuilder();

        text.AppendLine("📜 آخرین عملیات مدیریتی");
        text.AppendLine();

        if (entries.Count == 0)
        {
            text.AppendLine("هنوز عملیاتی ثبت نشده است.");
        }
        else
        {
            foreach (var entry in entries)
            {
                text.Append(
                    entry.Success ? "✅ " : "❌ "
                );

                text.Append(entry.Action);
                text.Append(" | ");
                text.Append(entry.AdminId);

                if (!string.IsNullOrWhiteSpace(
                        entry.TargetId))
                {
                    text.Append(" → ");
                    text.Append(entry.TargetId);
                }

                text.AppendLine();
                text.AppendLine(
                    FormatAdminTime(entry.CreatedAtMs)
                );

                if (!string.IsNullOrWhiteSpace(
                        entry.Details))
                {
                    string details = entry.Details;

                    if (details.Length > 100)
                        details = details[..100] + "…";

                    text.AppendLine(details);
                }

                text.AppendLine("────────");
            }
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🔄 تازه‌سازی",
                    "adm:audit"
                )
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🏠 خانه",
                    "adm:home"
                )
            }
        });

        return (text.ToString(), keyboard);
    }

    static async Task HandleAdminCallbackAsync(
        CallbackQuery callback,
        CancellationToken ct)
    {
        long userId = callback.From.Id;

        if (!IsPanelAdmin(userId))
        {
            await AnswerAdminCallback(
                callback,
                "⛔ دسترسی مدیریتی ندارید.",
                true,
                ct
            );
            return;
        }

        if (callback.Data == null ||
            callback.Message == null)
        {
            return;
        }

        Database.TouchAdmin(userId);

        string[] parts =
            callback.Data.Split(':');

        if (parts.Length < 2)
            return;

        string action = parts[1];

        if (action == "home")
        {
            await RenderAdminScreen(
                callback,
                BuildAdminHomeScreen(userId),
                ct
            );

            await AnswerAdminCallback(
                callback,
                null,
                false,
                ct
            );
            return;
        }

        if (action == "dash")
        {
            if (!CanAdmin(userId, "DASH"))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ دسترسی ندارید.",
                    true,
                    ct
                );
                return;
            }

            await RenderAdminScreen(
                callback,
                BuildAdminDashboardScreen(),
                ct
            );

            await AnswerAdminCallback(
                callback,
                null,
                false,
                ct
            );
            return;
        }

        if (action == "admins")
        {
            if (!IsPanelOwner(userId))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ فقط مالک اصلی.",
                    true,
                    ct
                );
                return;
            }

            int page =
                parts.Length >= 3 &&
                TryParseInt(parts[2], out int p)
                    ? p
                    : 0;

            await RenderAdminScreen(
                callback,
                BuildAdminListScreen(page),
                ct
            );

            await AnswerAdminCallback(
                callback,
                null,
                false,
                ct
            );
            return;
        }

        if (action == "add")
        {
            if (!IsPanelOwner(userId))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ فقط مالک اصلی.",
                    true,
                    ct
                );
                return;
            }

            adminInputRequests[userId] =
                new AdminInputRequest
                {
                    Kind = "add_admin",
                    ExpiresAtMs =
                        DateTimeOffset.UtcNow
                            .AddMinutes(5)
                            .ToUnixTimeMilliseconds()
                };

            var keyboard =
                new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton
                            .WithCallbackData(
                                "❌ لغو",
                                "adm:cancelinput"
                            )
                    }
                });

            await RenderAdminScreen(
                callback,
                (
                    "➕ افزودن مدیر\n\n" +
                    "آیدی عددی و نام مدیر را ارسال کنید.\n\n" +
                    "مثال:\n" +
                    "123456789 مدیر اقتصاد\n\n" +
                    "مدیر جدید بدون هیچ Permission ساخته می‌شود.",
                    keyboard
                ),
                ct
            );

            await AnswerAdminCallback(
                callback,
                null,
                false,
                ct
            );
            return;
        }

        if (action == "cancelinput")
        {
            adminInputRequests.TryRemove(
                userId,
                out _
            );

            await RenderAdminScreen(
                callback,
                BuildAdminHomeScreen(userId),
                ct
            );

            await AnswerAdminCallback(
                callback,
                "لغو شد.",
                false,
                ct
            );
            return;
        }

        if (action == "view" &&
            parts.Length >= 3 &&
            TryParseLong(parts[2], out long viewId))
        {
            if (!IsPanelOwner(userId))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ فقط مالک اصلی.",
                    true,
                    ct
                );
                return;
            }

            await RenderAdminScreen(
                callback,
                BuildAdminDetailsScreen(viewId),
                ct
            );

            await AnswerAdminCallback(
                callback,
                null,
                false,
                ct
            );
            return;
        }

        if (action == "perms" &&
            parts.Length >= 4 &&
            TryParseLong(parts[2], out long permissionId) &&
            TryParseInt(parts[3], out int permissionPage))
        {
            if (!IsPanelOwner(userId))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ فقط مالک اصلی.",
                    true,
                    ct
                );
                return;
            }

            await RenderAdminScreen(
                callback,
                BuildAdminPermissionScreen(
                    permissionId,
                    permissionPage
                ),
                ct
            );

            await AnswerAdminCallback(
                callback,
                null,
                false,
                ct
            );
            return;
        }

        if (action == "perm" &&
            parts.Length >= 5 &&
            TryParseLong(parts[2], out long permAdminId) &&
            TryParseInt(parts[4], out int permPage))
        {
            if (!IsPanelOwner(userId))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ فقط مالک اصلی.",
                    true,
                    ct
                );
                return;
            }

            string permissionCode = parts[3];

            if (!AdminPermissionCatalog.Exists(
                    permissionCode))
            {
                await AnswerAdminCallback(
                    callback,
                    "Permission نامعتبر.",
                    true,
                    ct
                );
                return;
            }

            var admin =
                Database.GetAdmin(permAdminId);

            if (admin == null || admin.IsOwner)
            {
                await AnswerAdminCallback(
                    callback,
                    "مدیر قابل ویرایش نیست.",
                    true,
                    ct
                );
                return;
            }

            bool currentlyEnabled =
                Database.HasAdminPermission(
                    permAdminId,
                    permissionCode
                );

            Database.SetAdminPermission(
                permAdminId,
                permissionCode,
                !currentlyEnabled,
                userId
            );

            Database.WriteAdminAudit(
                userId,
                currentlyEnabled
                    ? "PERMISSION_REVOKE"
                    : "PERMISSION_GRANT",
                "Admin",
                permAdminId.ToString(),
                permissionCode
            );

            await RenderAdminScreen(
                callback,
                BuildAdminPermissionScreen(
                    permAdminId,
                    permPage
                ),
                ct
            );

            await AnswerAdminCallback(
                callback,
                currentlyEnabled
                    ? "دسترسی حذف شد."
                    : "دسترسی اعطا شد.",
                false,
                ct
            );
            return;
        }

        if (action == "allask" &&
            parts.Length >= 4 &&
            TryParseLong(parts[2], out long allAskId) &&
            TryParseInt(parts[3], out int allAskValue))
        {
            if (!IsPanelOwner(userId))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ فقط مالک اصلی.",
                    true,
                    ct
                );
                return;
            }

            bool enableAll = allAskValue == 1;

            string text =
                enableAll
                    ? "⚠️ همه دسترسی‌ها، شامل Restore و حذف کشور، به این مدیر داده شود؟"
                    : "⚠️ تمام دسترسی‌های این مدیر حذف شود؟";

            var keyboard =
                new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton
                            .WithCallbackData(
                                "✅ تأیید نهایی",
                                $"adm:all:{allAskId}:{allAskValue}"
                            )
                    },
                    new[]
                    {
                        InlineKeyboardButton
                            .WithCallbackData(
                                "❌ انصراف",
                                $"adm:perms:{allAskId}:0"
                            )
                    }
                });

            await RenderAdminScreen(
                callback,
                (text, keyboard),
                ct
            );

            await AnswerAdminCallback(
                callback,
                null,
                false,
                ct
            );
            return;
        }

        if (action == "all" &&
            parts.Length >= 4 &&
            TryParseLong(parts[2], out long allId) &&
            TryParseInt(parts[3], out int allValue))
        {
            if (!IsPanelOwner(userId))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ فقط مالک اصلی.",
                    true,
                    ct
                );
                return;
            }

            bool enable = allValue == 1;

            Database.SetAllAdminPermissions(
                allId,
                enable,
                userId
            );

            Database.WriteAdminAudit(
                userId,
                enable
                    ? "PERMISSIONS_GRANT_ALL"
                    : "PERMISSIONS_REVOKE_ALL",
                "Admin",
                allId.ToString()
            );

            await RenderAdminScreen(
                callback,
                BuildAdminPermissionScreen(allId, 0),
                ct
            );

            await AnswerAdminCallback(
                callback,
                enable
                    ? "همه دسترسی‌ها اعطا شد."
                    : "همه دسترسی‌ها حذف شد.",
                true,
                ct
            );
            return;
        }

        if (action == "toggle" &&
            parts.Length >= 3 &&
            TryParseLong(parts[2], out long toggleId))
        {
            if (!IsPanelOwner(userId))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ فقط مالک اصلی.",
                    true,
                    ct
                );
                return;
            }

            var admin = Database.GetAdmin(toggleId);

            if (admin == null || admin.IsOwner)
            {
                await AnswerAdminCallback(
                    callback,
                    "مدیر قابل تغییر نیست.",
                    true,
                    ct
                );
                return;
            }

            bool newState = !admin.IsActive;

            Database.SetAdminActive(
                toggleId,
                newState
            );

            Database.WriteAdminAudit(
                userId,
                newState
                    ? "ADMIN_ACTIVATE"
                    : "ADMIN_DEACTIVATE",
                "Admin",
                toggleId.ToString()
            );

            await RenderAdminScreen(
                callback,
                BuildAdminDetailsScreen(toggleId),
                ct
            );

            await AnswerAdminCallback(
                callback,
                newState
                    ? "مدیر فعال شد."
                    : "مدیر غیرفعال شد.",
                false,
                ct
            );
            return;
        }

        if (action == "removeask" &&
            parts.Length >= 3 &&
            TryParseLong(parts[2], out long removeAskId))
        {
            if (!IsPanelOwner(userId))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ فقط مالک اصلی.",
                    true,
                    ct
                );
                return;
            }

            var admin =
                Database.GetAdmin(removeAskId);

            if (admin == null || admin.IsOwner)
            {
                await AnswerAdminCallback(
                    callback,
                    "مدیر قابل حذف نیست.",
                    true,
                    ct
                );
                return;
            }

            var keyboard =
                new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton
                            .WithCallbackData(
                                "🗑 بله، حذف شود",
                                $"adm:remove:{removeAskId}"
                            )
                    },
                    new[]
                    {
                        InlineKeyboardButton
                            .WithCallbackData(
                                "❌ انصراف",
                                $"adm:view:{removeAskId}"
                            )
                    }
                });

            string text =
                "⚠️ تأیید حذف مدیر\n\n" +
                $"نام: {admin.DisplayName}\n" +
                $"آیدی: {admin.AdminId}\n\n" +
                "تمام Permissionهای این مدیر نیز حذف می‌شود.";

            await RenderAdminScreen(
                callback,
                (text, keyboard),
                ct
            );

            await AnswerAdminCallback(
                callback,
                null,
                false,
                ct
            );
            return;
        }

        if (action == "remove" &&
            parts.Length >= 3 &&
            TryParseLong(parts[2], out long removeId))
        {
            if (!IsPanelOwner(userId))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ فقط مالک اصلی.",
                    true,
                    ct
                );
                return;
            }

            bool removed =
                Database.DeleteAdmin(removeId);

            Database.WriteAdminAudit(
                userId,
                "ADMIN_DELETE",
                "Admin",
                removeId.ToString(),
                "",
                removed
            );

            await RenderAdminScreen(
                callback,
                BuildAdminListScreen(0),
                ct
            );

            await AnswerAdminCallback(
                callback,
                removed
                    ? "مدیر حذف شد."
                    : "حذف انجام نشد.",
                !removed,
                ct
            );
            return;
        }

        if (action == "audit")
        {
            if (!CanAdmin(userId, "AUDIT"))
            {
                await AnswerAdminCallback(
                    callback,
                    "⛔ دسترسی ندارید.",
                    true,
                    ct
                );
                return;
            }

            await RenderAdminScreen(
                callback,
                BuildAdminAuditScreen(),
                ct
            );

            await AnswerAdminCallback(
                callback,
                null,
                false,
                ct
            );
            return;
        }

        if (action == "todo" &&
            parts.Length >= 3)
        {
            string module = parts[2];

            await AnswerAdminCallback(
                callback,
                $"ماژول «{AdminModuleTitle(module)}» در فاز بعد متصل می‌شود.",
                true,
                ct
            );
            return;
        }

        if (action == "close")
        {
            adminInputRequests.TryRemove(
                userId,
                out _
            );

            await AnswerAdminCallback(
                callback,
                "پنل بسته شد.",
                false,
                ct
            );

            DeleteNow(
                callback.Message.Chat.Id,
                callback.Message.MessageId
            );

            await SendPermanent(
                userId,
                "✅ پنل مدیریت بسته شد.",
                markup: new ReplyKeyboardRemove(),
                ct: ct
            );
            return;
        }

        await AnswerAdminCallback(
            callback,
            "دستور مدیریتی ناشناخته.",
            true,
            ct
        );
    }

    static async Task RenderAdminScreen(
        CallbackQuery callback,
        (
            string Text,
            InlineKeyboardMarkup Keyboard
        ) screen,
        CancellationToken ct)
    {
        if (callback.Message == null)
            return;

        try
        {
            await bot.EditMessageTextAsync(
                callback.Message.Chat.Id,
                callback.Message.MessageId,
                screen.Text,
                replyMarkup: screen.Keyboard,
                cancellationToken: ct
            );
        }
        catch (Exception ex)
        {
            if (!ex.Message.Contains(
                    "message is not modified",
                    StringComparison.OrdinalIgnoreCase))
            {
                await SendPermanent(
                    callback.From.Id,
                    screen.Text,
                    markup: screen.Keyboard,
                    ct: ct
                );
            }
        }
    }

    static async Task AnswerAdminCallback(
        CallbackQuery callback,
        string? text,
        bool showAlert,
        CancellationToken ct)
    {
        try
        {
            await bot.AnswerCallbackQueryAsync(
                callback.Id,
                text,
                showAlert: showAlert,
                cancellationToken: ct
            );
        }
        catch
        {
        }
    }

    static async Task SendAdminDenied(
        long userId,
        CancellationToken ct)
    {
        await SendPermanent(
            userId,
            "⛔ شما دسترسی لازم برای این بخش را ندارید.",
            ct: ct
        );
    }

    static async Task SendAdminModulePending(
        long userId,
        string moduleTitle,
        string permission,
        CancellationToken ct)
    {
        if (!CanAdmin(userId, permission))
        {
            await SendAdminDenied(userId, ct);
            return;
        }

        await SendPermanent(
            userId,
            $"🧩 ماژول «{moduleTitle}» در فاز بعدی پنل فعال می‌شود.",
            ct: ct
        );
    }

    static string AdminModuleTitle(string module) =>
        module switch
        {
            "players" => "پلیرها",
            "countries" => "کشورها",
            "groups" => "گروه‌ها",
            "alliances" => "اتحادها",
            "economy" => "اقتصاد",
            "war" => "جنگ",
            "operations" => "عملیات",
            "announce" => "اعلامیه",
            "settings" => "تنظیمات",
            "maintenance" => "نگهداری",
            _ => module
        };

    static string FormatAdminTime(long unixMs)
    {
        if (unixMs <= 0)
            return "-";

        return DateTimeOffset
            .FromUnixTimeMilliseconds(unixMs)
            .UtcDateTime
            .AddHours(3.5)
            .ToString("yyyy-MM-dd HH:mm");
    }
}

// ===== FINAL MERGED MODULES =====

// ----- ACTIVITY MODULE -----
static partial class Database
{
    private const long GroupActivityWriteThrottleMs = 60_000;

    private static readonly ConcurrentDictionary<long, long>
        groupActivityLastWrite = new();

    public static void InitActivity()
    {
        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS PlayerActivity(
                UserId INTEGER PRIMARY KEY,
                LastActiveMs INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_PlayerActivity_LastActiveMs
                ON PlayerActivity(LastActiveMs);

            CREATE TABLE IF NOT EXISTS GroupActivity(
                ChatId INTEGER PRIMARY KEY,
                LastActiveMs INTEGER NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_GroupActivity_LastActiveMs
                ON GroupActivity(LastActiveMs);

            CREATE INDEX IF NOT EXISTS IX_Countries_OwnerId
                ON Countries(OwnerId);
        ";

        cmd.ExecuteNonQuery();
    }

    public static void MarkPlayerActive(long userId)
    {
        if (userId == 0)
            return;

        try
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            using var con = OpenCon();
            using var cmd = con.CreateCommand();

            cmd.CommandText = @"
                INSERT INTO PlayerActivity(UserId, LastActiveMs)
                VALUES($userId, $nowMs)
                ON CONFLICT(UserId) DO UPDATE SET
                    LastActiveMs = excluded.LastActiveMs
                WHERE PlayerActivity.LastActiveMs
                    < excluded.LastActiveMs - 60000;
            ";

            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$nowMs", nowMs);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PLAYER ACTIVITY ERR] {ex.Message}");
        }
    }

    public static void MarkGroupActive(long chatId)
    {
        if (chatId == 0)
            return;

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (!ShouldWriteGroupActivity(chatId, nowMs))
            return;

        try
        {
            using var con = OpenCon();
            using var cmd = con.CreateCommand();

            cmd.CommandText = @"
                INSERT INTO GroupActivity(ChatId, LastActiveMs)
                VALUES($chatId, $nowMs)
                ON CONFLICT(ChatId) DO UPDATE SET
                    LastActiveMs = excluded.LastActiveMs;
            ";

            cmd.Parameters.AddWithValue("$chatId", chatId);
            cmd.Parameters.AddWithValue("$nowMs", nowMs);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GROUP ACTIVITY ERR] {ex.Message}");
        }
    }

    private static bool ShouldWriteGroupActivity(long chatId, long nowMs)
    {
        while (true)
        {
            if (!groupActivityLastWrite.TryGetValue(chatId, out long previous))
            {
                if (groupActivityLastWrite.TryAdd(chatId, nowMs))
                    return true;

                continue;
            }

            if (nowMs - previous < GroupActivityWriteThrottleMs)
                return false;

            if (groupActivityLastWrite.TryUpdate(chatId, nowMs, previous))
                return true;
        }
    }

    public static (
        int Players24h,
        int Groups24h,
        int Players7d,
        int Groups7d,
        int Players30d,
        int Groups30d
    ) GetActivityStats()
    {
        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        long since24h = nowMs - 24L * 60L * 60L * 1000L;
        long since7d = nowMs - 7L * 24L * 60L * 60L * 1000L;
        long since30d = nowMs - 30L * 24L * 60L * 60L * 1000L;

        using var con = OpenCon();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            SELECT
                (
                    SELECT COUNT(*)
                    FROM PlayerActivity p
                    WHERE p.LastActiveMs >= $since24h
                      AND EXISTS(
                          SELECT 1
                          FROM Countries c
                          WHERE c.OwnerId = p.UserId
                      )
                ),
                (
                    SELECT COUNT(*)
                    FROM GroupActivity g
                    WHERE g.LastActiveMs >= $since24h
                ),
                (
                    SELECT COUNT(*)
                    FROM PlayerActivity p
                    WHERE p.LastActiveMs >= $since7d
                      AND EXISTS(
                          SELECT 1
                          FROM Countries c
                          WHERE c.OwnerId = p.UserId
                      )
                ),
                (
                    SELECT COUNT(*)
                    FROM GroupActivity g
                    WHERE g.LastActiveMs >= $since7d
                ),
                (
                    SELECT COUNT(*)
                    FROM PlayerActivity p
                    WHERE p.LastActiveMs >= $since30d
                      AND EXISTS(
                          SELECT 1
                          FROM Countries c
                          WHERE c.OwnerId = p.UserId
                      )
                ),
                (
                    SELECT COUNT(*)
                    FROM GroupActivity g
                    WHERE g.LastActiveMs >= $since30d
                );
        ";

        cmd.Parameters.AddWithValue("$since24h", since24h);
        cmd.Parameters.AddWithValue("$since7d", since7d);
        cmd.Parameters.AddWithValue("$since30d", since30d);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return (0, 0, 0, 0, 0, 0);

        return (
            Convert.ToInt32(reader.GetInt64(0)),
            Convert.ToInt32(reader.GetInt64(1)),
            Convert.ToInt32(reader.GetInt64(2)),
            Convert.ToInt32(reader.GetInt64(3)),
            Convert.ToInt32(reader.GetInt64(4)),
            Convert.ToInt32(reader.GetInt64(5))
        );
    }
}

partial class Program
{
    static Timer? activityStatsTimer;

    static string BuildActivityStatsText()
    {
        var stats = Database.GetActivityStats();

        return
            "📊 آمار فعالیت\n\n" +

            "🕐 ۲۴ ساعت اخیر\n" +
            $"👤 پلیر فعال: {stats.Players24h}\n" +
            $"👥 گپ فعال: {stats.Groups24h}\n\n" +

            "📅 ۷ روز اخیر\n" +
            $"👤 پلیر فعال: {stats.Players7d}\n" +
            $"👥 گپ فعال: {stats.Groups7d}\n\n" +

            "🗓 ۳۰ روز اخیر\n" +
            $"👤 پلیر فعال: {stats.Players30d}\n" +
            $"👥 گپ فعال: {stats.Groups30d}";
    }

    static async Task SendActivityStats(
        long chatId,
        bool permanent,
        CancellationToken ct = default)
    {
        string text = BuildActivityStatsText();

        if (permanent)
        {
            await SendPermanent(chatId, text, ct: ct);
        }
        else
        {
            await SendTemp(chatId, text, ct: ct);
        }
    }

    static void StartActivityStatsTimer()
    {
        try
        {
            activityStatsTimer?.Dispose();
            activityStatsTimer = null;

            DateTime now = GetTehranNow();
            DateTime target = now.Date.AddHours(22);

            if (target <= now)
                target = target.AddDays(1);

            TimeSpan delay = target - now;

            activityStatsTimer = new Timer(async _ =>
            {
                try
                {
                    await SendActivityStats(
                        OWNER_ID,
                        permanent: true,
                        ct: CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[ACTIVITY STATS SEND ERR] {ex.Message}"
                    );
                }
                finally
                {
                    try
                    {
                        StartActivityStatsTimer();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"[ACTIVITY STATS RESCHEDULE ERR] {ex.Message}"
                        );
                    }
                }
            }, null, delay, Timeout.InfiniteTimeSpan);

            Console.WriteLine(
                $"[ACTIVITY STATS TIMER] next report: " +
                $"{target:yyyy-MM-dd HH:mm} Tehran"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[ACTIVITY STATS TIMER ERR] {ex.Message}"
            );

            activityStatsTimer?.Dispose();

            activityStatsTimer = new Timer(
                _ =>
                {
                    try
                    {
                        StartActivityStatsTimer();
                    }
                    catch
                    {
                    }
                },
                null,
                TimeSpan.FromMinutes(1),
                Timeout.InfiniteTimeSpan
            );
        }
    }
}

// ----- ATTACK GUIDES -----
partial class Program
{
    static string GroundAttackStrategyName(int strategy) =>
        strategy == 1
            ? "هجوم منسجم"
            : "محاصره و ضربه";

    static string GroundAttackTacticName(int strategy, int tactic) =>
        (strategy, tactic) switch
        {
            (1, 1) => "حمله مستقیم به قلب خط دفاع",
            (1, 2) => "حملات سبک هدف‌دار و هجوم سنگین متمرکز",
            (2, 1) => "حلقه محاصره با حملات پراکنده و هجوم سریع",
            (2, 2) => "حلقه محاصره متحرک و ضربات سنگین",
            _ => "تاکتیک نامشخص"
        };

    static readonly string GroundAttackStrategyGuide = """
⚔️ انتخاب استراتژی حمله زمینی

1️⃣ هجوم منسجم
نیروهای مهاجم به‌شکل منظم و متمرکز وارد نبرد می‌شوند تا با ایجاد فشار مستقیم، خط دفاع دشمن را بشکنند.

2️⃣ محاصره و ضربه
نیروها برای محدود کردن تحرک و ارتباط دشمن، خطوط دفاعی را محاصره می‌کنند و سپس با ضربات هماهنگ آن‌ها را فرسوده و نابود می‌کنند.
""";

    static string GroundAttackTacticGuide(int strategy)
    {
        if (strategy == 1)
        {
            return """
⚔️ استراتژی: هجوم منسجم

1️⃣ حمله مستقیم به قلب خط دفاع
تمام سربازان و تانک‌ها در یک نقطه متمرکز می‌شوند و در قالب چند واحد منظم پیشروی می‌کنند. هدف، درگیری مستقیم و شکستن خطوط غیرمتمرکز دشمن با ضربات سنگین است.

2️⃣ حملات سبک هدف‌دار و هجوم سنگین متمرکز
نیروها تقسیم می‌شوند. گروه‌های سبک با حملات هدف‌دار نظم دشمن را برهم می‌زنند و نقاط ضعف را آشکار می‌کنند؛ سپس ارتش اصلی به‌صورت متمرکز هجوم می‌برد و خط دفاع را می‌شکند.
""";
        }

        return """
⚔️ استراتژی: محاصره و ضربه

1️⃣ حلقه محاصره با حملات پراکنده و هجوم سریع
خطوط دفاعی دشمن کاملاً محاصره می‌شوند تا قدرت تحرک آن‌ها کاهش یابد. حملات پراکنده دشمن را فرسوده می‌کند و در پایان، هجوم سریع خطوط نامنظم را درهم می‌شکند.

2️⃣ حلقه محاصره متحرک و ضربات سنگین
دشمن در حلقه‌ای بزرگ گرفتار و ارتباطش با بیرون قطع می‌شود. ارتش از تمام جهات، آهسته اما هماهنگ پیشروی می‌کند و با ضربات سنگین گروه‌های کوچک را حذف و نیروهای باقی‌مانده را متراکم و بی‌حرکت می‌کند.
""";
    }

    static string AirAttackStrategyName(int strategy) =>
        strategy == 1
            ? "برتری هوایی"
            : "بمباران راهبردی";

    static string AirAttackTacticName(int strategy, int tactic) =>
        (strategy, tactic) switch
        {
            (1, 1) => "شکار آزاد (Freie Jagd)",
            (1, 2) => "حمله به پایگاه‌ها (Counter-air Strike)",
            (2, 1) => "بمباران دقیق (Precision Bombing)",
            (2, 2) => "بمباران منطقه‌ای (Area Bombing)",
            _ => "تاکتیک نامشخص"
        };

    static readonly string AirAttackStrategyGuide = """
🛫 انتخاب استراتژی حمله هوایی

1️⃣ برتری هوایی (Air Superiority)
هدف، از بین بردن توان هوایی دشمن و به‌دست گرفتن کنترل آسمان است.

2️⃣ بمباران راهبردی (Strategic Bombing)
هدف، تضعیف توان اقتصادی، صنعتی و روحیه دشمن با حمله به اهداف مهم در عمق قلمرو اوست.
""";

    static string AirAttackTacticGuide(int strategy)
    {
        if (strategy == 1)
        {
            return """
🛫 استراتژی: برتری هوایی

1️⃣ شکار آزاد (Freie Jagd)
جنگنده‌ها به‌صورت مستقل یا در گروه‌های کوچک به پشت خطوط دشمن نفوذ می‌کنند و هواپیماهای در حال پرواز، شامل جنگنده‌ها، بمب‌افکن‌ها و هواپیماهای شناسایی را هدف می‌گیرند.

2️⃣ حمله به پایگاه‌ها (Counter-air Strike)
فرودگاه‌ها، آشیانه‌ها، برج‌های مراقبت و انبارهای سوخت دشمن به‌شکل غافلگیرانه بمباران می‌شوند تا هواپیماهای دشمن پیش از برخاستن، روی زمین منهدم شوند.
""";
        }

        return """
🛫 استراتژی: بمباران راهبردی

1️⃣ بمباران دقیق (Precision Bombing)
اهداف کوچک و حیاتی مانند کارخانه‌های تسلیحات، پالایشگاه‌ها و ایستگاه‌های راه‌آهن انتخاب می‌شوند و از ارتفاع متوسط، با تمرکز بالا بمباران می‌شوند.

2️⃣ بمباران منطقه‌ای (Area Bombing)
گروه بزرگی از بمب‌افکن‌ها یک منطقه وسیع، مانند شهر یا منطقه صنعتی، را هدف می‌گیرند تا زیرساخت‌ها به‌طور گسترده تخریب و روحیه دشمن تضعیف شود.
""";
    }
}

// ----- DEFENSE GUIDES -----
partial class Program
{
    static string GroundDefenseStrategyName(int strategy) =>
        strategy == 1
            ? "دفاع منسجم"
            : "دفاع و ضدحمله پراکنده";

    static string GroundDefenseTacticName(int strategy, int tactic) =>
        (strategy, tactic) switch
        {
            (1, 1) => "دفاع ایستا و ثابت با قوای زرهی",
            (1, 2) => "گشت متحرک با گروه‌های ترکیبی",
            (2, 1) => "استتار و ضربه به گروه‌های پیشرو",
            (2, 2) => "عقب‌نشینی تاکتیکی و تله‌گذاری مخفی",
            _ => "تاکتیک نامشخص"
        };

    static readonly string GroundDefenseStrategyGuide = """
🛡 انتخاب استراتژی دفاع زمینی

1️⃣ دفاع منسجم
نیروهای مدافع در یک ساختار هماهنگ و نسبتاً ثابت مستقر می‌شوند تا خط دفاعی قدرتمندی ایجاد کنند و مانع نفوذ مستقیم دشمن شوند.

2️⃣ دفاع و ضدحمله پراکنده
نیروها با استتار، پراکندگی و عقب‌نشینی حساب‌شده، مهاجم را به عمق منطقه می‌کشانند و سپس با ضدحمله و محاصره به او ضربه می‌زنند.
""";

    static string GroundDefenseTacticGuide(int strategy)
    {
        if (strategy == 1)
        {
            return """
🛡 استراتژی: دفاع منسجم

1️⃣ دفاع ایستا و ثابت با قوای زرهی
سربازان در سنگرها و پشت موانع طبیعی مستقر می‌شوند و تانک‌ها در خط اول قرار می‌گیرند تا با آتش مستقیم، حرکت مهاجم را متوقف کنند.

2️⃣ گشت متحرک با گروه‌های ترکیبی
گروه‌های کوچک ترکیبی، متشکل از تانک و سرباز، به‌طور مداوم در خط مقدم حرکت می‌کنند تا نیروهای پراکنده مهاجم را شناسایی و هدف قرار دهند.
""";
        }

        return """
🛡 استراتژی: دفاع و ضدحمله پراکنده

1️⃣ استتار و ضربه به گروه‌های پیشرو
سربازان در بوته‌زارها، خرابه‌ها یا پشت تپه‌ها مخفی می‌شوند و تانک‌ها در سنگرهای پنهان و ثابت قرار می‌گیرند تا نیروی پیشرو دشمن غافلگیر شود و ضربه سنگینی دریافت کند.

2️⃣ عقب‌نشینی تاکتیکی و تله‌گذاری مخفی
بخشی از خطوط دفاعی عمداً خالی گذاشته می‌شود تا دشمن وارد عمق منطقه شود. سپس مسیرهای ارتباطی او مسدود و واحدهای مهاجم در محاصره و تله‌های مختلف گرفتار می‌شوند.
""";
    }

    static string AirDefenseStrategyName(int strategy) =>
        strategy == 1
            ? "دفاع منطقه‌ای (Area Defense)"
            : "دفاع نقطه‌ای (Point Defense)";

    static string AirDefenseTacticName(int strategy, int tactic) =>
        (strategy, tactic) switch
        {
            (1, 1) => "گشت هوایی رزمی (CAP)",
            (1, 2) => "ایستگاه‌های شنود و هشدار سریع",
            (2, 1) => "آتشبند (Flak Barrage)",
            (2, 2) => "پوشش مستقیم جنگنده (Close Escort)",
            _ => "تاکتیک نامشخص"
        };

    static readonly string AirDefenseStrategyGuide = """
🛫 انتخاب استراتژی دفاع هوایی

1️⃣ دفاع منطقه‌ای (Area Defense)
هدف، حفاظت از یک منطقه وسیع مانند کشور یا جبهه بزرگ با پراکندگی نیروها و رهگیری تهدیدها پیش از رسیدن به اهداف حساس است.

2️⃣ دفاع نقطه‌ای (Point Defense)
تمرکز نیروهای دفاعی بر حفاظت از اهداف حیاتی و محدود مانند شهرها، کارخانه‌ها، پایگاه‌ها و تأسیسات مهم است.
""";

    static string AirDefenseTacticGuide(int strategy)
    {
        if (strategy == 1)
        {
            return """
🛫 استراتژی: دفاع منطقه‌ای

1️⃣ گشت هوایی رزمی (CAP)
جنگنده‌ها به‌طور مداوم در آسمان منطقه گشت می‌زنند تا هواپیماهای دشمن را پیش از رسیدن به اهداف حساس شناسایی و رهگیری کنند.

2️⃣ ایستگاه‌های شنود و هشدار سریع 🔒
رادارهای زمینی و تجهیزات شنود، حرکت دشمن را کشف می‌کنند و جنگنده‌ها را به سمت تهدید هدایت می‌کنند.

این تاکتیک در وضعیت فعلی آلیس به رادار نیاز دارد و قفل است.
""";
        }

        return """
🛫 استراتژی: دفاع نقطه‌ای

1️⃣ آتشبند (Flak Barrage)
توپ‌های ضدهوایی به‌صورت متراکم در اطراف هدف مستقر می‌شوند و با آتش متوالی و سنگین، مسیر پرواز هواپیماهای دشمن را مسدود می‌کنند.

2️⃣ پوشش مستقیم جنگنده (Close Escort)
جنگنده‌های دفاعی در مجاورت هدف حیاتی، مانند کارخانه یا پایگاه، گشت می‌زنند و در لحظه حمله مستقیماً وارد درگیری می‌شوند.
""";
    }
}
