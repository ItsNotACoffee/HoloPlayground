using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Random = System.Random;
using DigitalRuby.LightningBolt;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Rendering;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

/**
 * Glavni kontroler za 3D weather aplikaciju
 */
public class WeatherController : MonoBehaviour
{
    // Lista vremena: Thunderstorm, Drizzle, Rain, Snow, (Mist, Smoke, Haze, Dust, Fog, Sand, Ash, Squall, Tornado), Clear, Clouds
    private const string API_KEY = "8f9108e39ee415df720f40caddb9e68a";
    private string CITY_ID = "3188383"; // Defaultna vrijednost: Varaždin, HR
    private string TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

    [Header("Panels")]
    public GameObject entirePanelObject;
    public GameObject weatherPanel;
    public GameObject forecastPanel;

    private WeatherInfo weatherInfo;
    private Forecast forecast;
    private List<GameObject> forecastScrollableObjects = new List<GameObject>(); //drži sve forecast objekte osim originala koji se klonira

    [Header("Forecast tab objects")]
    public GameObject forecastObject;
    public GameObject forecastGridCollection;
    public GameObject scrollingObjectCollection;
    public GameObject loadingForecastText;

    private List<GameObject> weatherObjects = new List<GameObject>();

    [Header("3D model objects")]
    public GameObject snowObject;
    public GameObject heavyCloudsObject;
    public GameObject lightCloudsObject;
    public GameObject rainObject;
    public GameObject fogObject;
    public GameObject weather3DModel;
    public GameObject clearObject;
    public GameObject lightning1Object;
    public GameObject lightning2Object;
    public GameObject lightning3Object;

    [Header("Search panel objects")]
    public TextMeshPro noCityFoundText;
    public TextMeshPro searchText;
    private TouchScreenKeyboard keyboard;
    private string customCityId;

    [Header("Misc. objects")]
    public TextAsset jsonFile; //JSON file sa svim ID-jevima gradova
    private Cities cities;

    public TextMeshPro infoText;
    public Image weatherIcon;

    private int lightningTimer = 0;
    private int lightingMaxTimer = 1000;

    // originalne transform koordinate 3D modela relativne panelima da se može resetirati prilikom paljenja i gašenja modela
    private Vector3 modelPosition;

    private void Start()
    {
        weatherObjects.Add(snowObject);
        weatherObjects.Add(heavyCloudsObject);
        weatherObjects.Add(lightCloudsObject);
        weatherObjects.Add(rainObject);
        weatherObjects.Add(fogObject);
        weatherObjects.Add(clearObject);

        // pobrini se da objekti koji trebaju biti isklju?eni u startu stvarno jesu
        weather3DModel.SetActive(false);
        forecastPanel.SetActive(false);
        forecastObject.SetActive(false);

        weatherPanel.SetActive(true);

        ReadCities();
        if (weatherPanel.activeSelf)
        {
            StartCoroutine(FetchWeather(UpdateWeather));
        }
        if (forecastPanel.activeSelf)
        {
            StartCoroutine(FetchForecast(UpdateForecast));
        }

        // reorijentiraj scenu prema kameri
        this.transform.root.gameObject.GetComponent<MixedRealitySceneContent>().ReorientContent();

        // mora se dohvatiti poslije orijentacije
        modelPosition = weather3DModel.transform.localPosition;
    }

    private void Update()
    {
        if (keyboard != null)
        {
            searchText.text = keyboard.text;
        }
        GenerateLightning();
    }

    #region Public methods

    /**
     * Pali/gasi panel sa vremenom
     */
    public void SwitchToWeatherPanel()
    {
        forecastPanel.SetActive(false);
        weatherPanel.SetActive(true);
        // ažuriraj vrijeme ako ve? nije
        if (weatherInfo == null)
        {
            infoText.text = "loading...";
            StartCoroutine(FetchWeather(UpdateWeather));
        }
    }

    /**
     * Pali/gasi panel sa 5-dnevnom prognozom
     */
    public void SwitchToForecastPanel()
    {
        weatherPanel.SetActive(false);
        forecastPanel.SetActive(true);
        // ažuriraj prognozu ako ve? nije
        if (forecast == null)
        {
            loadingForecastText.SetActive(true);
            StartCoroutine(FetchForecast(UpdateForecast));
        }
    }

    /**
     * Osvježava sve trenutno aktivne panele i vremenske podatke
     */
    public void RefreshWeather()
    {
        weatherInfo = null;
        forecast = null;

        if (weatherPanel.activeSelf)
        {
            infoText.text = "loading...";
            StartCoroutine(FetchWeather(UpdateWeather));
        }
        if (forecastPanel.activeSelf)
        {
            loadingForecastText.SetActive(true);
            StartCoroutine(FetchForecast(UpdateForecast));
        }
    }

    /**
     * Pretražuje grad po imenu unesenom od strane korisnika.
     * Ako je grad na?en, poziva se osvježavanje vremena.
     */
    public void SearchCityName()
    {
        // ime grada se postavlja u Update() metodi svaki frame dok korisnik piše na tipkovnici
        string inputText = searchText.text;
        string cityId = null;
        foreach (City city in cities.cities)
        {
            if (inputText != null && inputText.Length > 0 && city.name.ToLower().Equals(inputText.ToLower()))
            {
                cityId = city.id;
                break;
            }
        }
        if (cityId != null)
        {
            noCityFoundText.text = "";
            customCityId = cityId;
            RefreshWeather();
        }
        else
        {
            noCityFoundText.text = "No city found with that name.";
        }
    }

    /**
     * Poziva pretraživanje grada imenom ako je kliknut jedan od preset gradova ispod pretraživanja
     */
    public void SearchPresetCity(string cityName)
    {
        searchText.text = cityName;
        SearchCityName();
    }

    /**
     * Pali/gasi bound kontrolu 3D modela
     */
    public void ToggleModelBoundControl()
    {
        if (weather3DModel.activeSelf)
        {
            if (weather3DModel.GetComponent<BoundsControl>().enabled)
            {
                weather3DModel.GetComponent<BoundsControl>().enabled = false;
            }
            else
            {
                weather3DModel.GetComponent<BoundsControl>().enabled = true;
            }

        }
    }

    /**
     * Poziva native tastaturu sustava te ju prikazuje.
     * Ako nije dostupna, ne radi ništa.
     */
    public void OpenSystemKeyboard()
    {
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false);
    }

    /**
     * Pali gasi 3D vremenski model.
     */
    public void Toggle3DModel()
    {
        if (weather3DModel.activeSelf)
        {
            weather3DModel.SetActive(false);
        }
        else
        {
            weather3DModel.transform.localPosition = modelPosition;
            weather3DModel.SetActive(true);
        }
    }

    /**
     * Pokre?e 'Tap to place'
     */
    public void EnableTapToPlace()
    {
        entirePanelObject.GetComponent<TapToPlace>().enabled = true;
        entirePanelObject.GetComponent<TapToPlace>().StartPlacement();
    }

    #endregion

    #region Private methods

    /**
     * Poziva OpenWeatherMap API i dohva?a podatke o trenutnom vremenu uklju?uju?i ikonu.
     */
    private IEnumerator FetchWeather(Action<WeatherInfo> onSuccess)
    {
        // dohvati vrijeme
        UnityWebRequest request = UnityWebRequest.Get(String.Format("http://api.openweathermap.org/data/2.5/weather?id={0}&APPID={1}&units=metric", customCityId != null ? customCityId : CITY_ID, API_KEY));
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(request.error);
        }
        else
        {
            byte[] result = request.downloadHandler.data;
            string jsonResponse = System.Text.Encoding.Default.GetString(result);
            WeatherInfo info = JsonUtility.FromJson<WeatherInfo>(jsonResponse);
            request.Dispose();

            // dohvati i ikonu
            request = UnityWebRequestTexture.GetTexture(String.Format("https://openweathermap.org/img/wn/{0}@2x.png", info.weather[0].icon));
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(request.error);
            }
            else
            {
                Texture2D imgTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite imgSprite = Sprite.Create(imgTexture, new Rect(0, 0, imgTexture.width, imgTexture.height), new Vector2(.5f, .5f));
                weatherIcon.sprite = imgSprite;
                request.Dispose();
                onSuccess(info);
            }
        }
    }

    /**
     * Poziva OpenWeatherMap API i dohva?a podatke o 5-dnevnoj prognozi
     */
    private IEnumerator FetchForecast(Action<Forecast> onSuccess)
    {
        // dohvati prognozu
        UnityWebRequest request = UnityWebRequest.Get(String.Format("http://api.openweathermap.org/data/2.5/forecast?id={0}&APPID={1}&units=metric", customCityId != null ? customCityId : CITY_ID, API_KEY));
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(request.error);
        }
        else
        {
            byte[] result = request.downloadHandler.data;
            string jsonResponse = System.Text.Encoding.Default.GetString(result);
            Forecast forecast = JsonUtility.FromJson<Forecast>(jsonResponse);
            request.Dispose();
            onSuccess(forecast);
        }
    }

    /**
     * Ažurira 3D model vremena te podatke koji se prikazuju na glavnom tabu ovisno o proslije?enom WeatherInfo objektu.
     */
    private void UpdateWeather(WeatherInfo info)
    {
        this.weatherInfo = info;

        // izaberi objekte koji se prikazuju na 3D modelu.
        switch (weatherInfo.weather[0].main)
        {
            case "Clear":
                {
                    clearObject.SetActive(true);
                    foreach (GameObject weatherObject in weatherObjects)
                    {
                        if (!weatherObject.Equals(clearObject))
                        {
                            weatherObject.SetActive(false);
                        }
                    }
                    break;
                }
            case "Clouds":
                {
                    clearObject.SetActive(true); // This is used for audio only
                    lightCloudsObject.SetActive(true);
                    foreach (GameObject weatherObject in weatherObjects)
                    {
                        if (!weatherObject.Equals(lightCloudsObject) && !weatherObject.Equals(clearObject))
                        {
                            weatherObject.SetActive(false);
                        }
                    }
                    break;
                }
            case "Drizzle":
            case "Rain":
                {
                    heavyCloudsObject.SetActive(true);
                    rainObject.SetActive(true);
                    foreach (GameObject weatherObject in weatherObjects)
                    {
                        if (!weatherObject.Equals(heavyCloudsObject) && !weatherObject.Equals(rainObject))
                        {
                            weatherObject.SetActive(false);
                        }
                    }
                    break;
                }
            case "Thunderstorm":
                {
                    heavyCloudsObject.SetActive(true);
                    rainObject.SetActive(true);
                    lightning1Object.SetActive(true);
                    lightning2Object.SetActive(true);
                    lightning3Object.SetActive(true);
                    foreach (GameObject weatherObject in weatherObjects)
                    {
                        if (!weatherObject.Equals(heavyCloudsObject) && !weatherObject.Equals(rainObject)
                            && !weatherObject.Equals(lightning1Object) && !weatherObject.Equals(lightning2Object) && !weatherObject.Equals(lightning3Object))
                        {
                            weatherObject.SetActive(false);
                        }
                    }
                    break;
                }
            case "Snow":
                {
                    heavyCloudsObject.SetActive(true);
                    snowObject.SetActive(true);
                    foreach (GameObject weatherObject in weatherObjects)
                    {
                        if (!weatherObject.Equals(heavyCloudsObject) && !weatherObject.Equals(snowObject))
                        {
                            weatherObject.SetActive(false);
                        }
                    }
                    break;
                }
            case "Mist":
            case "Smoke":
            case "Haze":
            case "Dust":
            case "Fog":
            case "Sand":
                {
                    fogObject.SetActive(true);
                    foreach (GameObject weatherObject in weatherObjects)
                    {
                        if (!weatherObject.Equals(fogObject))
                        {
                            weatherObject.SetActive(false);
                        }
                    }
                    break;
                }
        }

        // složi vremenski info koji ?e se prikazati
        string temp = weatherInfo.main.temp.Contains(".") ? weatherInfo.main.temp.Split(".")[0] : weatherInfo.main.temp;
        string tempFeelsLike = weatherInfo.main.feels_like.Contains(".") ? weatherInfo.main.feels_like.Split(".")[0] : weatherInfo.main.feels_like;
        string textString = "Location: " + weatherInfo.name + ", " + weatherInfo.sys.country + "<br>";
        textString += "Weather: " + weatherInfo.weather[0].main + "<br>";
        textString += "Description: " + weatherInfo.weather[0].description + "<br>";
        textString += "Temperature: " + temp + "C, " + "feels like " + tempFeelsLike + "C<br>";
        textString += "Pressure: " + weatherInfo.main.pressure + " hPa<br>";
        textString += "Humidity: " + weatherInfo.main.humidity + " %<br>";
        infoText.text = textString;
    }

    /**
     * Ažurira 5-dnevnu prognozu sa proslije?enim Forecat objektom
     */
    private void UpdateForecast(Forecast forecast)
    {
        this.forecast = forecast;

        // dealociraj trenutnu listu prognoze koja se prikazuje
        foreach (GameObject forecastObj in forecastScrollableObjects)
        {
            Destroy(forecastObj);
        }
        forecastScrollableObjects.Clear();

        StartCoroutine(ContinueForecast(forecast.list));
    }

    /**
     * Nastavlja ažuriranje prognoze dohva?anjem ikone za svaku te rekreiranjem scroll panela sa dohva?enim podacima
     */
    private IEnumerator ContinueForecast(List<Flist> forecastList)
    {
        // generiraj jedan scrollabilni element po jednom objektu
        foreach (Flist hourlyWeather in forecast.list)
        {
            GameObject forecastClone = Instantiate(forecastObject, forecastGridCollection.transform);

            GameObject timeText = forecastClone.transform.Find("TimeText").gameObject;
            GameObject temperatureText = forecastClone.transform.Find("TemperatureText").gameObject;
            GameObject sprite = forecastClone.transform.Find("Sprite").gameObject;

            // dohvati ikonu
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(String.Format("https://openweathermap.org/img/wn/{0}@2x.png", hourlyWeather.weather[0].icon));
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(request.error);
            }
            else
            {
                Texture2D imgTexture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite imgSprite = Sprite.Create(imgTexture, new Rect(0, 0, imgTexture.width, imgTexture.height), new Vector2(.5f, .5f));
                sprite.GetComponent<SpriteRenderer>().sprite = imgSprite;

                // parsiraj vrijeme
                DateTime parsedDate;
                if (DateTime.TryParseExact(hourlyWeather.dt_txt, TIME_FORMAT, null, System.Globalization.DateTimeStyles.None, out parsedDate))
                {
                    timeText.GetComponent<TextMeshPro>().text = parsedDate.ToString("g"); //g = 1.1.2022. 18:00
                }

                // parsiraj temperaturu
                string temp_min = hourlyWeather.main.temp_min.Contains(".") ? hourlyWeather.main.temp_min.Split(".")[0] : hourlyWeather.main.temp_min;
                string temp_max = hourlyWeather.main.temp_max.Contains(".") ? hourlyWeather.main.temp_max.Split(".")[0] : hourlyWeather.main.temp_max;
                temperatureText.GetComponent<TextMeshPro>().text = temp_min + "C" + " / " + temp_max + "C";

                // koristi cache kako se ne bi dešavale greške vezane za više instanci renderera
                timeText.GetComponent<MaterialInstance>().CacheSharedMaterialsFromRenderer = true;
                temperatureText.GetComponent<MaterialInstance>().CacheSharedMaterialsFromRenderer = true;

                forecastScrollableObjects.Add(forecastClone);

                request.Dispose();
            }
        }

        loadingForecastText.SetActive(false);
        foreach (GameObject forecastClone in forecastScrollableObjects)
        {
            forecastClone.SetActive(true);
        }

        // pozovi obje update metode. Rješava bug da se ne može scrollati ako se ažuriraju elementi.
        forecastGridCollection.GetComponent<GridObjectCollection>().UpdateCollection();
        scrollingObjectCollection.GetComponent<ScrollingObjectCollection>().UpdateContent();
    }

    /**
     * Nasumi?no generira munju na jednu od 3 lokacije u 3D modelu.
     * Ova metoda se pokre?e svaki frame te generira munju svakih nekoliko sekundi ako je vrijeme postavljeno na Thunderstorm.
     */
    private void GenerateLightning()
    {
        if (weatherInfo != null && weatherInfo.weather[0].main.Equals("Thunderstorm") && weather3DModel.activeSelf)
        {
            if (lightningTimer == lightingMaxTimer)
            {
                lightningTimer = 0;
                Random rand = new Random();
                int lightningIndex = rand.Next(1, 4);
                lightingMaxTimer = rand.Next(1000, 4000); // minimalno 100 frameova izme?u munja

                switch (lightningIndex)
                {
                    case 1:
                        lightning1Object.GetComponent<LightningBoltScript>().Trigger();
                        lightning1Object.GetComponent<AudioSource>().Play();
                        break;
                    case 2:
                        lightning2Object.GetComponent<LightningBoltScript>().Trigger();
                        lightning2Object.GetComponent<AudioSource>().Play();
                        break;
                    case 3:
                        lightning3Object.GetComponent<LightningBoltScript>().Trigger();
                        lightning3Object.GetComponent<AudioSource>().Play();
                        break;
                }
            }
            else
            {
                lightningTimer++;
            }
        }
    }

    /**
     * ?ita i sprema podatke o gradovima iz JSON datoteke. Pokre?e se na po?etku aplikacije.
     */
    private void ReadCities()
    {
        // unity podržava samo objekte kao top-level grane u JSON datoteci,
        // pa je potrebno dodati root granu kako bi se datoteka mogla pro?itati.
        cities = JsonUtility.FromJson<Cities>("{\"cities\":" + jsonFile.text + "}");
        Debug.Log("Successfully loaded city list");
    }

    #endregion

    #region Objects

    // Data klase za vrijeme
    [Serializable]
    public class WeatherInfo
    {
        public int id;
        public string name;
        public List<Weather> weather;
        public Main main;
        public Wind wind;
        public Sys sys;
    }

    [Serializable]
    public class Weather
    {
        public int id;
        public string main;
        public string description;
        public string icon;
    }

    [Serializable]
    public class Main
    {
        public string temp;
        public string feels_like;
        public string pressure;
        public string humidity;
        public string temp_min;
        public string temp_max;
    }

    [Serializable]
    public class Wind
    {
        public string speed;
        public string deg;
    }

    [Serializable]
    public class Sys
    {
        public string country;
    }

    [Serializable]
    public class City
    {
        public string id;
        public string name;
        public string country;
    }

    [Serializable]
    public class Cities
    {
        public City[] cities;
    }

    // Data klase za prognozu
    [Serializable]
    public class Forecast
    {
        public string cod;
        public string cnt;
        public List<Flist> list;
    }

    [Serializable]
    public class Flist
    {
        public string dt;
        public string dt_txt;
        public Main main;
        public List<Weather> weather;
    }

    #endregion
}
