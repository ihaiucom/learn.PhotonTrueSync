// attached to each room on join list to do a join if the players hit the button
using System;
using System.Collections.Generic;
using System.IO;
using TrueSync;
using UnityEngine;

public class ReplayUtils {

    /**
    * @brief Represents a specific context for save and load players.
    * 
    * It can be used to separate different levels or lobbies of the game.
    **/
    public static string replayContext;

    /**
    * @brief Folder to save/load replays.
    **/
    private const string REPLAY_FOLDER = "replays";

    public static void Init() {
        ReplayRecord.ReplayRecordSave += SaveRecord;
    }

    /**
    * @brief Saves the provided replay.
    **/
    private static void SaveRecord(byte[] replayRecord, int numberOfPlayers) {
        string folderPath = CheckReplayFolder().FullName;

        #if !UNITY_WEBPLAYER
        try {
            File.WriteAllBytes(string.Format("{0}/replay_{1}_{2}.tsr", folderPath, DateTime.Now.Ticks, numberOfPlayers), replayRecord);
        } catch (Exception) {
        }
        #endif
    }

    /**
    * @brief Returns a sorted by date list of {@link ReplayRecordInfo} of the saved replay found.
    **/
    public static List<ReplayRecordInfo> GetContextRecords() {
        List<ReplayRecordInfo> result = new List<ReplayRecordInfo>();

        #if !UNITY_WEBPLAYER
        foreach (FileInfo fileInfo in CheckReplayFolder().GetFiles("*.tsr")) {
            ReplayRecordInfo replay = new ReplayRecordInfo(fileInfo);
            if (replay != null) {
                result.Add(replay);
            }
        }

        result.Sort();
        #endif

        return result;
    }

    /**
    * @brief Returns a instance of {@from ReplayRecord} saved in the provided file location.
    **/
    public static ReplayRecord GetReplayRecord(string fileFullName) {
        ReplayRecord replay = null;

        #if !UNITY_WEBPLAYER
        try {
            byte[] replayContent = File.ReadAllBytes(fileFullName);
            replay = ReplayRecord.GetReplayRecord(replayContent);
        } catch (Exception) {
        }
        #endif

        return replay;
    }

    /**
     * @brief Clear all replay records saved.
     **/
    public static void ClearAllReplayRecords() {
        #if !UNITY_WEBPLAYER
        foreach (FileInfo fileInfo in CheckReplayFolder().GetFiles("*.tsr")) {
            fileInfo.Delete();
        }
        #endif
    }

    /**
    * @brief Checks whether the replay's folder is created, it creates it otherwise.
    **/
    private static DirectoryInfo CheckReplayFolder() {
        string path = null;
        if (replayContext != null && replayContext.Trim().Length > 0) {
            path = string.Format("{0}/{1}/{2}", Application.persistentDataPath, REPLAY_FOLDER, replayContext);
        } else {
            path = string.Format("{0}/{1}", Application.persistentDataPath, REPLAY_FOLDER);
        }

        DirectoryInfo folderInfo = new DirectoryInfo(path);

        if (!folderInfo.Exists) {
            folderInfo.Create();
        }

        return folderInfo;
    }

}

/**
* @brief Provides basic information about a {@link ReplayRecord}.
**/
public class ReplayRecordInfo : IComparable<ReplayRecordInfo> {

    /**
     * @brief Creation time of the replay.
     **/
    public DateTime creationDate;

    /**
     * @brief FullName of replay's file.
     **/
    public string fileFullName;

    /**
     * @brief Number of players.
     **/
    public int numberOfPlayers = 1;

    public ReplayRecordInfo(FileInfo fileInfo) {
        #if !UNITY_WEBPLAYER
        this.creationDate = fileInfo.CreationTime;
        #endif
        this.fileFullName = fileInfo.FullName;

        string[] nameSplited = fileInfo.Name.Split('_');
        if (nameSplited.Length >= 3) {
            numberOfPlayers = int.Parse(nameSplited[2].Split('.')[0]);
        }
    }

    /**
     * @brief Returns the related {@link ReplayRecord}.
     **/
    public void Load() {
        ReplayRecord.replayToLoad = ReplayUtils.GetReplayRecord(fileFullName);
    }

    /**
     * @brief Comparison based on creation date (desc order).
     **/
    public int CompareTo(ReplayRecordInfo other) {
        return other.creationDate.CompareTo(this.creationDate);
    }

}