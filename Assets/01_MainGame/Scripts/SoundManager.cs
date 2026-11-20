using UnityEngine;

public enum BgmType
{
    None,
    Battle_Downtown_1,  // Stage 1,2 用
    Battle_Downtown_2,  // Stage 3 用
    Battle_Casino_1,    // Stage 4,5 用
    Battle_Casino_2,    // Stage 6 用
    Battle_FinalBoss,   // Stage 7 用
    Shop_1,             // Shop 1 用
    Shop_2,             // Shop 2 用
    Map                 // マップ用
}

public enum SeType
{
    // ダイス
    DiceThrow,      // 投下音
    DiceBounce,     // バウンド
    DiceLock,       // キープ
    DiceUnLock,     // キープ解除
    DicePut,        // ダイスの着地音

    // エフェクト
    HandNone,       // ブタ
    HandCommon,     // 通常役
    HandBig,        // 大技
    HandJackpot,    // ファイブダイス
    ScoreCount,     // スコア加算
    CoinGet,        // コイン獲得
    CheatUse,       // イカサマ発動
    MapJump,        // マップ移動音

    // シール
    SealAttach,     // シール貼り付け

    // システム
    StageClear,     // ステージクリア
    GameOver,       // 返済失敗
    ShopBuy,        // ショップ購入
    Choice          // 選択音
}

public class SoundManager : MonoBehaviour
{
    // ===== フィールド =====
    private static SoundManager _instance;

    [Header("音量設定")]
    [Range(0f, 1f)] public float BGMVolume = 0.6f;
    [Range(0f, 1f)] public float SEVolume = 0.8f;

    [Header("BGM")]
    [SerializeField] private AudioClip _bgmDowntown1;
    [SerializeField] private AudioClip _bgmDowntown2;
    [SerializeField] private AudioClip _bgmCasino1;
    [SerializeField] private AudioClip _bgmCasino2;
    [SerializeField] private AudioClip _bgmFinalBoss;
    [SerializeField] private AudioClip _bgmShop1;
    [SerializeField] private AudioClip _bgmShop2;
    [SerializeField] private AudioClip _bgmMap;

    [Header("ダイスSE")]
    [SerializeField] private AudioClip _diceShake1;
    [SerializeField] private AudioClip _diceShake2;
    [SerializeField] private AudioClip _seDiceThrow;
    [SerializeField] private AudioClip _seDiceBounce;
    [SerializeField] private AudioClip _seDiceLock;
    [SerializeField] private AudioClip _seDiceUnLock;
    [SerializeField] private AudioClip _seDicePut;

    [Header("エフェクトSE")]
    [SerializeField] private AudioClip _seHandNone;
    [SerializeField] private AudioClip _seHandCommon;
    [SerializeField] private AudioClip _seHandBig;
    [SerializeField] private AudioClip _seHandJackpot;
    [SerializeField] private AudioClip _seScoreCount;
    [SerializeField] private AudioClip _seCheatUse;
    [SerializeField] private AudioClip _seCoinGet;
    [SerializeField] private AudioClip _seMapJump;

    [Header("シールSE")]
    [SerializeField] private AudioClip _seSealAttach;

    [Header("システムSE")]
    [SerializeField] private AudioClip _seStageClear;
    [SerializeField] private AudioClip _seGameOver;
    [SerializeField] private AudioClip _seShopBuy;
    [SerializeField] private AudioClip _seChoice;

    private AudioSource _bgmSource;
    private AudioSource _seSource;
    private AudioSource _shakeSource1;
    private AudioSource _shakeSource2;

    private BgmType _currentBgmType = BgmType.None;

    // ===== プロパティ =====
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SoundManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[SoundManager]");
                    _instance = obj.AddComponent<SoundManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    // ===== Unityメッセージ =====
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSource();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    // ===== メソッド =====
    private void SetupAudioSource()
    {
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;

        _seSource = gameObject.AddComponent<AudioSource>();

        _shakeSource1 = gameObject.AddComponent<AudioSource>();
        _shakeSource1.loop = true;
        _shakeSource1.clip = _diceShake1;

        _shakeSource2 = gameObject.AddComponent<AudioSource>();
        _shakeSource2.loop = true;
        _shakeSource2.clip = _diceShake2;
    }

    // BGM再生
    public void PlayBGM(BgmType type)
    {
        if (_currentBgmType == type && _bgmSource.isPlaying) return;

        AudioClip clip = GetBgmClip(type);
        if (clip == null) return;

        _currentBgmType = type;
        _bgmSource.clip = clip;
        _bgmSource.volume = BGMVolume;
        _bgmSource.Play();
    }

    // BGM停止
    public void StopBGM(BgmType type)
    {
        _bgmSource.Stop();
        _currentBgmType = BgmType.None;
    }

    private AudioClip GetBgmClip(BgmType type)
    {
        switch (type)
        {
            case BgmType.Battle_Downtown_1: return _bgmDowntown1;
            case BgmType.Battle_Downtown_2: return _bgmDowntown2;
            case BgmType.Battle_Casino_1: return _bgmCasino1;
            case BgmType.Battle_Casino_2: return _bgmCasino2;
            case BgmType.Battle_FinalBoss: return _bgmFinalBoss;
            case BgmType.Shop_1: return _bgmShop1;
            case BgmType.Shop_2: return _bgmShop2;
            case BgmType.Map: return _bgmMap;
            default: return null;
        }
    }

    // ダイスジャラジャラ開始
    public void StartDiceShake()
    {
        if (_shakeSource1.clip != null && !_shakeSource1.isPlaying)
        {
            _shakeSource1.volume = SEVolume * 0.8f;
            _shakeSource1.Play();
        }

        if (_shakeSource2.clip != null && !_shakeSource2.isPlaying)
        {
            _shakeSource2.volume = SEVolume * 0.8f;
            _shakeSource2.Play();
        }
    }

    // ジャラジャラ停止
    public void StopDiceShake()
    {
        _shakeSource1.Stop();
        _shakeSource2.Stop();
    }

    // 単一のSE再生
    public void PlaySE(SeType type, float volumeScale = 1.0f)
    {
        AudioClip clip = GetSeClip(type);
        if (clip != null)
        {
            _seSource.PlayOneShot(clip, SEVolume * volumeScale);
        }
    }

    // 複数のSEを同時に鳴らす
    public void PlayMultipleSE(params SeType[] types)
    {
        if (types == null) return;
        foreach (var type in types)
        {
            PlaySE(type);
        }
    }

    // 音量バランスを指定して複数のSEを同時に鳴らす
    public void PlayMultipleSE(params (SeType type, float volume)[] soundList)
    {
        if (soundList == null) return;
        foreach (var item in soundList)
        {
            PlaySE(item.type, item.volume);
        }
    }

    private AudioClip GetSeClip(SeType type)
    {
        switch (type)
        {
            case SeType.DiceThrow: return _seDiceThrow;
            case SeType.DiceBounce: return _seDiceBounce;
            case SeType.DiceLock: return _seDiceLock;
            case SeType.DiceUnLock: return _seDiceUnLock;
            case SeType.DicePut: return _seDicePut;

            case SeType.HandNone: return _seHandNone;
            case SeType.HandCommon: return _seHandCommon;
            case SeType.HandBig: return _seHandBig;
            case SeType.HandJackpot: return _seHandJackpot;
            case SeType.ScoreCount: return _seScoreCount;
            case SeType.CoinGet: return _seCoinGet;
            case SeType.CheatUse: return _seCheatUse;
            case SeType.MapJump: return _seMapJump;

            case SeType.SealAttach: return _seSealAttach;

            case SeType.StageClear: return _seStageClear;
            case SeType.GameOver: return _seGameOver;
            case SeType.ShopBuy: return _seShopBuy;
            case SeType.Choice: return _seChoice;
            default: return null;
        }
    }
}
