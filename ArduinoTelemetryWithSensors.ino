/*
  Arduino Telemetri Sistemi - Ger�ek Sens�r Entegrasyonu
  
  Bu versiyon ger�ek sens�rlerle kullan�m i�in tasarlanm��t�r.
  MPU6050, BMP280, GPS mod�l� gibi sens�rlerle kullanabilirsiniz.
*/

#include <Wire.h>
// Ger�ek sens�rler i�in gerekli k�t�phaneleri buraya ekleyin:
// #include <MPU6050.h>
// #include <BMP280.h>
// #include <SoftwareSerial.h>

// Sabitler
const byte PACKET_HEADER[] = {0xAA, 0xBB, 0xCC, 0xDD};
const int PACKET_SIZE = 96;
const unsigned long SEND_INTERVAL = 1000; // 1 saniye
const byte TEAM_ID = 255;

// Timing
byte packetCounter = 0;
unsigned long lastSendTime = 0;

// Sens�r pinleri (�rnek)
const int TEMP_SENSOR_PIN = A0;
const int PRESSURE_SENSOR_PIN = A1;
const int HUMIDITY_SENSOR_PIN = A2;

// Telemetri de�i�kenleri
struct TelemetryData {
  float rocketAltitude;
  float payloadAltitude;
  float rocketLatitude;
  float rocketLongitude;
  float payloadLatitude;
  float payloadLongitude;
  float gyroX, gyroY, gyroZ;
  float accelX, accelY, accelZ;
  float angle;
  float rocketTemp, payloadTemp;
  float rocketPressure, payloadPressure;
  float payloadHumidity;
  float rocketSpeed, payloadSpeed;
  byte status;
} telemetry;

void setup() {
  Serial.begin(115200);
  Wire.begin();
  
  // Analog pinleri giri� olarak ayarla
  pinMode(TEMP_SENSOR_PIN, INPUT);
  pinMode(PRESSURE_SENSOR_PIN, INPUT);
  pinMode(HUMIDITY_SENSOR_PIN, INPUT);
  
  // Sens�rleri ba�lat
  initializeSensors();
  
  Serial.println("Ger�ek Sens�r Telemetri Sistemi Ba�lat�ld�");
  Serial.println("Paket boyutu: 96 byte");
  Serial.println("G�nderim aral���: 1 saniye");
  
  // �lk de�erleri ayarla
  telemetry.status = 1;
  telemetry.rocketLatitude = 39.925533; // Varsay�lan Ankara koordinatlar�
  telemetry.rocketLongitude = 32.866287;
  telemetry.payloadLatitude = 39.925533;
  telemetry.payloadLongitude = 32.866287;
  
  delay(2000);
}

void loop() {
  unsigned long currentTime = millis();
  
  if (currentTime - lastSendTime >= SEND_INTERVAL) {
    // Sens�r verilerini oku
    readSensorData();
    
    // Paketi g�nder
    sendTelemetryPacket();
    
    lastSendTime = currentTime;
    packetCounter++;
    
    // Debug ��kt�s�
    printSensorData();
  }
  
  delay(10);
}

void initializeSensors() {
  // Ger�ek sens�r ba�latma kodlar� buraya gelecek
  
  /* MPU6050 �rne�i:
  mpu.initialize();
  if (mpu.testConnection()) {
    Serial.println("MPU6050 ba�lant�s� ba�ar�l�");
  }
  */
  
  /* BMP280 �rne�i:
  if (bmp.begin()) {
    Serial.println("BMP280 ba�lat�ld�");
  }
  */
  
  Serial.println("Sens�rler ba�lat�ld� (sim�lasyon modu)");
}

void readSensorData() {
  // 1. �VME�L�ER VE J�ROSKOP (MPU6050)
  readIMUData();
  
  // 2. BASIN� VE SICAKLIK (BMP280)
  readPressureData();
  
  // 3. GPS VER�LER�
  readGPSData();
  
  // 4. �RT�FA HESAPLAMA
  calculateAltitude();
  
  // 5. HIZ HESAPLAMA
  calculateSpeed();
  
  // 6. NEM SENS�R�
  readHumidityData();
  
  // 7. DURUM G�NCELLEMES�
  updateStatus();
}

void readIMUData() {
  // Ger�ek MPU6050 okuma kodu:
  /*
  int16_t ax, ay, az, gx, gy, gz;
  mpu.getMotion6(&ax, &ay, &az, &gx, &gy, &gz);
  
  telemetry.accelX = ax / 16384.0 * 9.81; // m/s�'ye �evir
  telemetry.accelY = ay / 16384.0 * 9.81;
  telemetry.accelZ = az / 16384.0 * 9.81;
  
  telemetry.gyroX = gx / 131.0; // derece/saniye
  telemetry.gyroY = gy / 131.0;
  telemetry.gyroZ = gz / 131.0;
  
  // A�� hesaplama
  telemetry.angle = atan2(telemetry.accelY, telemetry.accelZ) * 180 / PI;
  */
  
  // Sim�lasyon (ger�ek sens�r ba�lanana kadar):
  float t = millis() / 1000.0;
  telemetry.accelX = sin(t * 0.2) * 2.0;
  telemetry.accelY = cos(t * 0.15) * 1.5;
  telemetry.accelZ = 9.81 + sin(t * 0.1) * 0.5;
  
  telemetry.gyroX = sin(t * 0.3) * 10.0;
  telemetry.gyroY = cos(t * 0.25) * 8.0;
  telemetry.gyroZ = sin(t * 0.1) * 5.0;
  
  telemetry.angle = atan2(telemetry.accelY, telemetry.accelZ) * 180 / PI;
}

void readPressureData() {
  // Ger�ek BMP280 okuma kodu:
  /*
  telemetry.rocketPressure = bmp.readPressure() / 100.0; // hPa
  telemetry.rocketTemp = bmp.readTemperature(); // �C
  */
  
  // Analog sens�r okuma �rne�i:
  int tempRaw = analogRead(TEMP_SENSOR_PIN);
  int pressureRaw = analogRead(PRESSURE_SENSOR_PIN);
  
  // Ham de�erleri ger�ek de�erlere �evir (sens�re g�re ayarlay�n)
  telemetry.rocketTemp = (tempRaw * 5.0 / 1024.0 - 0.5) * 100; // LM35 i�in
  telemetry.payloadTemp = telemetry.rocketTemp + random(-2, 3);
  
  // Bas�n� sim�lasyonu (ger�ek sens�r ba�lanana kadar)
  telemetry.rocketPressure = 1013.25 - (telemetry.rocketAltitude * 0.12);
  telemetry.payloadPressure = telemetry.rocketPressure + random(-5, 5);
}

void readGPSData() {
  // Ger�ek GPS mod�l� okuma kodu:
  /*
  if (gps.location.isValid()) {
    telemetry.rocketLatitude = gps.location.lat();
    telemetry.rocketLongitude = gps.location.lng();
  }
  */
  
  // Sim�lasyon - hafif hareket
  telemetry.rocketLatitude += (random(-10, 10) / 1000000.0);
  telemetry.rocketLongitude += (random(-10, 10) / 1000000.0);
  telemetry.payloadLatitude = telemetry.rocketLatitude + (random(-20, 20) / 1000000.0);
  telemetry.payloadLongitude = telemetry.rocketLongitude + (random(-20, 20) / 1000000.0);
}

void calculateAltitude() {
  // Bas�n�-irtifa d�n���m�
  float seaLevelPressure = 1013.25;
  telemetry.rocketAltitude = 44330 * (1.0 - pow(telemetry.rocketPressure / seaLevelPressure, 0.1903));
  
  // Payload irtifas� (roket irtifas�ndan biraz farkl�)
  telemetry.payloadAltitude = telemetry.rocketAltitude + random(-50, 20);
  
  // Negatif irtifa kontrol�
  if (telemetry.rocketAltitude < 0) telemetry.rocketAltitude = 0;
  if (telemetry.payloadAltitude < 0) telemetry.payloadAltitude = 0;
}

void calculateSpeed() {
  // �vme�l�er verilerinden h�z hesaplama (basitle�tirilmi�)
  static float lastTime = 0;
  static float lastAltitude = 0;
  
  float currentTime = millis() / 1000.0;
  float deltaTime = currentTime - lastTime;
  
  if (deltaTime > 0) {
    telemetry.rocketSpeed = (telemetry.rocketAltitude - lastAltitude) / deltaTime;
    telemetry.payloadSpeed = telemetry.rocketSpeed + random(-5, 5);
  }
  
  lastTime = currentTime;
  lastAltitude = telemetry.rocketAltitude;
}

void readHumidityData() {
  // Ger�ek nem sens�r� okuma:
  /*
  int humidityRaw = analogRead(HUMIDITY_SENSOR_PIN);
  telemetry.payloadHumidity = map(humidityRaw, 0, 1023, 0, 100);
  */
  
  // Sim�lasyon
  telemetry.payloadHumidity = 45 + sin(millis() / 10000.0) * 20;
  telemetry.payloadHumidity = constrain(telemetry.payloadHumidity, 0, 100);
}

void updateStatus() {
  float missionTime = millis() / 1000.0;
  
  if (missionTime < 5) {
    telemetry.status = 0; // Ba�lang��
  } else if (telemetry.rocketAltitude > 10) {
    telemetry.status = 1; // U�u�
  } else {
    telemetry.status = 2; // Yerde
  }
}

void sendTelemetryPacket() {
  byte packet[PACKET_SIZE];
  int index = 0;
  
  // Header
  for (int i = 0; i < 4; i++) {
    packet[index++] = PACKET_HEADER[i];
  }
  
  // Packet Counter
  packet[index++] = packetCounter;
  
  // T�m float de�erleri ekle
  addFloatToPacket(packet, index, telemetry.rocketAltitude);
  addFloatToPacket(packet, index, telemetry.rocketLatitude);
  addFloatToPacket(packet, index, telemetry.rocketLongitude);
  addFloatToPacket(packet, index, telemetry.payloadAltitude);
  addFloatToPacket(packet, index, telemetry.payloadLatitude);
  addFloatToPacket(packet, index, telemetry.payloadLongitude);
  
  addFloatToPacket(packet, index, telemetry.gyroX);
  addFloatToPacket(packet, index, telemetry.gyroY);
  addFloatToPacket(packet, index, telemetry.gyroZ);
  
  addFloatToPacket(packet, index, telemetry.accelX);
  addFloatToPacket(packet, index, telemetry.accelY);
  addFloatToPacket(packet, index, telemetry.accelZ);
  
  addFloatToPacket(packet, index, telemetry.angle);
  addFloatToPacket(packet, index, telemetry.rocketTemp);
  addFloatToPacket(packet, index, telemetry.payloadTemp);
  addFloatToPacket(packet, index, telemetry.rocketPressure);
  addFloatToPacket(packet, index, telemetry.payloadPressure);
  addFloatToPacket(packet, index, telemetry.payloadHumidity);
  addFloatToPacket(packet, index, telemetry.rocketSpeed);
  addFloatToPacket(packet, index, telemetry.payloadSpeed);
  
  // Status ve CRC
  packet[index++] = telemetry.status;
  packet[index++] = calculateCRC(packet, 4, 82);
  
  // Padding
  while (index < PACKET_SIZE) {
    packet[index++] = 0x00;
  }
  
  // G�nder
  Serial.write(packet, PACKET_SIZE);
  Serial.flush();
}

void addFloatToPacket(byte* packet, int& index, float value) {
  union {
    float f;
    byte b[4];
  } floatUnion;
  
  floatUnion.f = value;
  
  for (int i = 0; i < 4; i++) {
    packet[index++] = floatUnion.b[i];
  }
}

byte calculateCRC(byte* data, int offset, int length) {
  byte crc = 0;
  for (int i = offset; i < offset + length; i++) {
    crc ^= data[i];
  }
  return crc;
}

void printSensorData() {
  Serial.print("Paket #");
  Serial.print(packetCounter);
  Serial.print(" | Alt: ");
  Serial.print(telemetry.rocketAltitude, 1);
  Serial.print("m | Temp: ");
  Serial.print(telemetry.rocketTemp, 1);
  Serial.print("�C | Bas�n�: ");
  Serial.print(telemetry.rocketPressure, 1);
  Serial.print("hPa | Durum: ");
  Serial.println(telemetry.status);
}

/*
  GER�EK SENS�R ENTEGRASYONU ���N:
  
  1. Gerekli k�t�phaneleri y�kleyin:
     - MPU6050 i�in: MPU6050 library
     - BMP280 i�in: Adafruit BMP280 library
     - GPS i�in: TinyGPS++ library
  
  2. Sens�rleri ba�lay�n:
     - MPU6050: SDA->A4, SCL->A5 (I2C)
     - BMP280: SDA->A4, SCL->A5 (I2C)
     - GPS: RX->Pin2, TX->Pin3 (SoftwareSerial)
     - Analog sens�rler: A0, A1, A2
  
  3. Sim�lasyon kodlar�n� ger�ek sens�r okuma kodlar� ile de�i�tirin
  
  4. Kalibrasyon de�erlerini ayarlay�n
  
  Bu kod yap�s� ger�ek sens�rlerle kolayca entegre edilebilir!
*/