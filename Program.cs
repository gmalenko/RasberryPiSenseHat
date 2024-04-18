using System;
using System.Drawing;
using System.Threading;
using Iot.Device.Common;
using Iot.Device.SenseHat;
using UnitsNet;
using Iot.Device.Ht1632;
using MySql.Data.MySqlClient;

// set this to the current sea level pressure in the area for correct altitude readings
var defaultSeaLevelPressure = WeatherHelper.MeanSeaLevel;

using SenseHat sh = new SenseHat();
int n = 0;
int x = 3, y = 3;

var dbStuff = new DatabaseStuff();

while (true)
{
    Console.Clear();

    (int dx, int dy, bool holding) = JoystickState(sh);

    if (holding)
    {
        n++;
    }

    x = (x + 8 + dx) % 8;
    y = (y + 8 + dy) % 8;

    sh.Fill(n % 2 == 0 ? Color.DarkBlue : Color.DarkRed);
    sh.SetPixel(x, y, Color.Yellow);

    var tempValue = sh.Temperature;
    var temp2Value = sh.Temperature2;
    var preValue = sh.Pressure;
    var humValue = sh.Humidity;
    var accValue = sh.Acceleration;
    var angValue = sh.AngularRate;
    var magValue = sh.MagneticInduction;
    var altValue = WeatherHelper.CalculateAltitude(preValue, defaultSeaLevelPressure, tempValue);

    var senseValues = new SenseValues()
    
    {
        Temperature1 = tempValue.DegreesFahrenheit,
        Temperature2 = temp2Value.DegreesFahrenheit,
        Humidity = humValue.Percent,
        Altitude = altValue.Meters,
        DewPointTemperature1 = WeatherHelper.CalculateDewPoint(tempValue, humValue).DegreesFahrenheit,
        DewPointTemperature2 = WeatherHelper.CalculateDewPoint(temp2Value, humValue).DegreesFahrenheit,
        HeatIndexTemperature1 = WeatherHelper.CalculateHeatIndex(tempValue, humValue).DegreesFahrenheit,
        HeatIndexTemperature2 = WeatherHelper.CalculateHeatIndex(temp2Value, humValue).DegreesFahrenheit,
    };

    //Console.WriteLine($"Temperature 1: {senseValues.Temperature1}");
    //Console.WriteLine($"Temperature 2: {senseValues.Temperature2}");
    //Console.WriteLine($"Humidity: {senseValues.Humidity}");
    //Console.WriteLine($"Altitude: {senseValues.Altitude}");
    //Console.WriteLine($"DewPoint: {senseValues.DewPointTemperature1}");
    //Console.WriteLine($"DewPoint: {senseValues.DewPointTemperature2}");


    //Console.WriteLine($"Time: { time.ToString()}");
    //Console.WriteLine($"Temperature Sensor 1: {tempValue.DegreesCelsius:0.#}\u00B0C");
    //Console.WriteLine($"Temperature Sensor 2: {temp2Value.DegreesCelsius:0.#}\u00B0C");
    //Console.WriteLine($"Pressure: {preValue.Hectopascals:0.##} hPa");
    //Console.WriteLine($"Altitude: {altValue.Meters:0.##} m");
    //Console.WriteLine($"Acceleration: {sh.Acceleration} g");
    //Console.WriteLine($"Angular rate: {sh.AngularRate} DPS");
    //Console.WriteLine($"Magnetic induction: {sh.MagneticInduction} gauss");
    //Console.WriteLine($"Relative humidity: {humValue.Percent:0.#}%");
    //Console.WriteLine($"Heat index: {WeatherHelper.CalculateHeatIndex(tempValue, humValue).DegreesCelsius:0.#}\u00B0C");
    //Console.WriteLine($"Dew point: {WeatherHelper.CalculateDewPoint(tempValue, humValue).DegreesCelsius:0.#}\u00B0C");

    dbStuff.InsertData(senseValues);
    Thread.Sleep(1000);
}

(int, int, bool) JoystickState(SenseHat sh)
{
    sh.ReadJoystickState();

    int dx = 0;
    int dy = 0;

    if (sh.HoldingUp)
    {
        dy--; // y goes down
    }

    if (sh.HoldingDown)
    {
        dy++;
    }

    if (sh.HoldingLeft)
    {
        dx--;
    }

    if (sh.HoldingRight)
    {
        dx++;
    }

    return (dx, dy, sh.HoldingButton);
}

public class SenseValues
{
    public double Temperature1 { get; set; }
    public double Temperature2 { get; set; }
    public double Humidity { get; set; }
    public double Altitude {get;set;}
    public double HeatIndexTemperature1 {get;set;}
    public double HeatIndexTemperature2 {get;set;}
    public double DewPointTemperature1{get;set;}
    public double DewPointTemperature2{get;set;}

}

public class DatabaseStuff
{
    string connectionString = "Server=localhost;Database=SenseHat;Uid=user;Pwd=password;";
    public void InsertData(SenseValues senseValues)
    {
        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string insertQuery = @"INSERT INTO TemperatureValues
                                        (Temperature1, Temperature2, Humidity, Altitude, HeatIndexTemperature1, HeatIndexTemperature2, DewPointTemperature1, DewPointTemperature2, DateTimeCaptured)
                                        VALUES (@Temperature1, @Temperature2, @Humidity, @Altitude, @HeatIndexTemperature1, @HeatIndexTemperature2, @DewPointTemperature1, @DewPointTemperature2, CONVERT_TZ(NOW(), 'UTC', 'America/New_York'))";

                using (var command = new MySqlCommand(insertQuery,connection))
                {
                    command.Parameters.AddWithValue("@Temperature1", senseValues.Temperature1);
                    command.Parameters.AddWithValue("@Temperature2", senseValues.Temperature2);
                    command.Parameters.AddWithValue("@Humidity", senseValues.Humidity);
                    command.Parameters.AddWithValue("@Altitude", senseValues.Altitude);
                    command.Parameters.AddWithValue("@HeatIndexTemperature1", senseValues.HeatIndexTemperature1);
                    command.Parameters.AddWithValue("@HeatIndexTemperature2", senseValues.HeatIndexTemperature2);
                    command.Parameters.AddWithValue("@DewPointTemperature1", senseValues.DewPointTemperature1);
                    command.Parameters.AddWithValue("@DewPointTemperature2", senseValues.DewPointTemperature2);

                    command.ExecuteNonQuery();
                    Console.WriteLine("Data inserted successfully!");
                }                      
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
