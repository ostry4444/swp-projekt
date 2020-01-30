using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace swp_projekt
{
    static class DBconnector
    {
        private static string connstring = @"server=remotemysql.com;userid=CPll0ykcHj;password=TtEMtkjJsy;database=CPll0ykcHj";
        
        static MySqlConnection connection = new MySqlConnection(connstring);
        
        private static bool OpenConnection(){
            try{
                connection.Open();
                return true;
            }
            catch (MySqlException ex){
                switch (ex.Number){
                    case 0:
                        Console.WriteLine("Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        Console.WriteLine("Invalid username/password");
                        break;
                }
                return false;
            }
        }

        private static bool CloseConnection(){
            try{
                connection.Close();
                return true;
            }
            catch (MySqlException ex){
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public static bool connectionTest() {
            try{
                connection = new MySqlConnection(connstring);
                connection.Open();
                string query = "SELECT NOW();";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                Console.WriteLine( cmd.ExecuteScalar().ToString() );
                connection.Close();
                return true;
            }
            catch (Exception e){
                Console.WriteLine("Error: {0}", e.ToString());
                return false;
            }
            finally{
                if (connection != null){
                    connection.Close();
                }
            }
        }
        
        private static bool createTableTaxiOrder()
        {
            string query = @"CREATE TABLE TAXI_ORDERS ( 
                            id INT(9) UNSIGNED AUTO_INCREMENT PRIMARY KEY,
                            address VARCHAR(100),
                            orderTime DATETIME,
                            seats int,
                            phone int,
                            name VARCHAR(50)
                         )"; 

            try { 
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch(Exception e){
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        private static bool ifTableExists(String tableName)
        {
            try{
                string query = "SELECT * from " + tableName + ";";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.CommandTimeout = 200;
                cmd.ExecuteScalar();
                return true;
            }
            catch (MySqlException e){
                if (e.ErrorCode.Equals(-2147467259)) 
                    Console.WriteLine("Table " + tableName + " doesn't exist" );
                else Console.WriteLine(e.ToString());
                
                return false;
            }            
        }

        public static bool InsertTaxiOrder(TaxiOrder taxiOrder)
        {
            if (OpenConnection() == true){ 
                string format = "yyyy-MM-dd HH:mm:ss";
                bool created = true;
                
                if (!ifTableExists("TAXI_ORDERS")){
                    created = createTableTaxiOrder();
                }
                if (created){
                    string query = "INSERT INTO TAXI_ORDERS (address, orderTime, seats, phone) VALUES("
                            + "'" + taxiOrder.street.ToString()+" "+taxiOrder.addressNumber.ToString()+ "', '" + taxiOrder.getDateTime().ToString(format) + "', "
                            + taxiOrder.seats + ", " + taxiOrder.phone 
                            + ")";
                    Console.WriteLine(query);
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.ExecuteNonQuery();
                    CloseConnection();
                    return true;
                }
                else Console.WriteLine("problem with creating table TAXI_ORDER");
            }
            return false;
        }

        public static List<string> ReadStreets()
        {
            String tableName = "STREETS";
            String streetName = "NAZWA_ULICY";

            if (OpenConnection() == true)
            {
                List<string> addressList = new List<string>();

                if (ifTableExists(tableName))
                {
                    string query = "SELECT DISTINCT " + streetName + " FROM " + tableName + ";";

                    Console.WriteLine(query);
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    while (dataReader.Read())
                    {
                        addressList.Add(dataReader[streetName].ToString());
                    }

                    dataReader.Close();
                    CloseConnection();
                }
                else Console.WriteLine("DBConncetor - table " + tableName + " missing");

                return addressList;
            }
            else Console.WriteLine("DB connection problem");

            return null;
        }

        /*
         * returns lattitude and longitude of street stored in database. 
         * can be called with exact addres number (exact lat long) or just street name (returns first found lat long)
         */
        public static double[] getLatLong(String street, String number)
        {
            String TABLE_NAME = "STREETS";
            String STREET_NAME = "NAZWA_ULICY";
            String STREET_NUMBER = "NUMER";
            String LAT = "SZER_GEOGR";
            String LONG = "DL_GEOGR";

            if (OpenConnection() == true)
            {
                double[] latlong= new double[2];

                if (ifTableExists(TABLE_NAME))
                {
                    string query;
                    if (!String.IsNullOrEmpty(number))
                        query = "SELECT " + LAT + ", " + LONG + " FROM " + TABLE_NAME + " WHERE " + STREET_NAME+"=\""+street+"\" AND "+STREET_NUMBER+"=\""+number+"\";";
                    else                    
                        query = "SELECT " + LAT + ", " + LONG + " FROM " + TABLE_NAME + " WHERE " + STREET_NAME + "=\"" + street+"\";";
                    
                    Console.WriteLine(query);
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.CommandTimeout = 200;
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    if (dataReader.Read()){
                        Double.TryParse(dataReader[LAT].ToString(), out latlong[0]);
                        Double.TryParse(dataReader[LONG].ToString(), out latlong[1]);
                    }

                    dataReader.Close();
                    CloseConnection();
                }
                else Console.WriteLine("DBConncetor getLatLong() - table " + TABLE_NAME + " missing");

                return latlong;
            }
            else Console.WriteLine("DB connection problem");

            return null;
        }

        public static List <TaxiOrder> getTaxiOrders()
        {
            if (OpenConnection() == true)
            {
                List<TaxiOrder> taxiOrdersList = new List<TaxiOrder>();

                if (ifTableExists("TAXI_ORDERS"))
                {
                    string query = "SELECT * FROM TAXI_ORDERS ORDER BY id DESC;";

                    Console.WriteLine(query);
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    while (dataReader.Read())
                    {
                        TaxiOrder taxiOrder = new TaxiOrder();
                            taxiOrder.street        = dataReader["address"].ToString();
                            taxiOrder.dateTimeStr   = dataReader["orderTime"].ToString();
                            taxiOrder.seats         = Int32.Parse(dataReader["seats"].ToString());
                            taxiOrder.phone         = Int32.Parse (dataReader["phone"].ToString());
                        taxiOrdersList.Add(taxiOrder);
                    }

                    dataReader.Close();
                    CloseConnection();
                }
                else Console.WriteLine("DBConncetor - table TAXI_ORDERS missing");

                return taxiOrdersList;
            }
            else Console.WriteLine("DB connection problem");

            return null;
        }

    }
}