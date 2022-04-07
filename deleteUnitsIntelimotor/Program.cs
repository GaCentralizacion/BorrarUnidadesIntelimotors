using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace deleteUnitsIntelimotor
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Program myobject = new Program();
                DataTable unidades = new DataTable();

                unidades = myobject.ConsultaDB($"SELECT idUnidad, numeroSerie, idEmpresa, idSucursal FROM [Reporte].[deleteUnitIntelimotor]");

                if (unidades.Rows.Count > 0)
                {
                    foreach (DataRow u in unidades.Rows)
                    {
                        string IdIntelimotor = IdUnidadIntelimotor(u.ItemArray[1].ToString(), u.ItemArray[2].ToString(), u.ItemArray[3].ToString());

                        if (!string.IsNullOrEmpty(IdIntelimotor))
                        {
                            int error = IdIntelimotor.IndexOf("Error");
                            if (error == -1)
                            {
                                string respuesta = Borrar(IdIntelimotor);
                                //string respuesta = "";
                                if (!string.IsNullOrEmpty(respuesta))
                                {
                                    myobject.ConsultaDB($"INSERT INTO [Reporte].[bitacoraDeleteUnitIntelimotor] VALUES({u.ItemArray[0].ToString()}, '{respuesta}', GETDATE())");
                                    myobject.ConsultaDB($"DELETE FROM [Reporte].[deleteUnitIntelimotor] WHERE idUnidad = {u.ItemArray[0].ToString()}");
                                }
                                else
                                {
                                    myobject.ConsultaDB($"INSERT INTO [Reporte].[bitacoraDeleteUnitIntelimotor] VALUES({u.ItemArray[0].ToString()}, 'No se obtuvo respuesta al borrar', GETDATE())");
                                };
                            }
                            else {
                                myobject.ConsultaDB($"INSERT INTO [Reporte].[bitacoraDeleteUnitIntelimotor] VALUES({u.ItemArray[0].ToString()}, '{IdIntelimotor}', GETDATE())");
                            };
                        }
                        else
                        {
                            myobject.ConsultaDB($"INSERT INTO [Reporte].[bitacoraDeleteUnitIntelimotor] VALUES({u.ItemArray[0].ToString()}, 'No se obtuvo ID', GETDATE())");
                        };
                    };
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }


        public static string IdUnidadIntelimotor(string VIN, string Empresa, string Sucursal)
        {
            string IdIntelimotor = string.Empty;
            try
            {
                string url = @"https://app.intelimotor.com/api/integrations/andrade/units/" + VIN + "/" + Empresa + "/" + Sucursal;
                url += "?apiKey=3bbce11b44d5&apiSecret=MYvlQ4W5Lq4ftXnyVxreX6dpG0nTpufC5p542mz5CZrzDDfun1a";

                HttpWebRequest request = HttpWebRequest.CreateHttp(url);
                request.Method = "GET";
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                        {

                            string responseJSON = myStreamReader.ReadToEnd();
                            JObject json = JObject.Parse(responseJSON);

                            foreach (JObject EventX in json["data"])
                            {
                                string id = (string)EventX["id"];
                                bool disponible = (bool)EventX["isSold"];
                                if (!disponible)
                                {
                                    IdIntelimotor = id;
                                    break;
                                }
                                else {
                                    IdIntelimotor = "Error No se encontro el isSold en falso";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                IdIntelimotor = "Error " + VIN + ": " + ex.Message;
            }
            return IdIntelimotor;
        }

        public static string Borrar(string IdIntelimotor)
        {
            string respuesta = string.Empty;
            try
            {
                string url = @"https://app.intelimotor.com/api/units/" + IdIntelimotor;
                url += "?apiKey=3bbce11b44d5&apiSecret=MYvlQ4W5Lq4ftXnyVxreX6dpG0nTpufC5p542mz5CZrzDDfun1a";

                HttpWebRequest request = HttpWebRequest.CreateHttp(url);
                request.Method = "DELETE";
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            string responseJSON = myStreamReader.ReadToEnd();
                            JObject json = JObject.Parse(responseJSON);

                            respuesta = json["data"]["id"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                respuesta = ex.Message;
            }
            return respuesta;
        }

        public DataTable ConsultaDB(string LsQuery)
        {
            DataTable dtConsulta = new DataTable();

            SqlConnection ConnectionDB = new SqlConnection("Data Source=192.168.20.29; Initial Catalog=Desflote; User ID=sa;Password=S0p0rt3; Connection Timeout=20000");
            ConnectionDB.Open();
            SqlCommand ComandoDB = new SqlCommand(LsQuery, ConnectionDB);

            SqlDataAdapter DatosGet = new SqlDataAdapter(ComandoDB);
            DatosGet.Fill(dtConsulta);
            ConnectionDB.Close();

            return dtConsulta;

        }
    }
}
