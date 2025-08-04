/*
  Arduino Telemetri Sistemi - Gerçek Sensör Entegrasyonu
  
  Bu versiyon gerçek sensörlerle kullaným için tasarlanmýþtýr.
  MPU6050, BMP280, GPS modülü gibi sensörlerle kullanabilirsiniz.
*/

#include <Wire.h>
// Gerçek sensörler için gerekli kütüphaneleri buraya ekleyin:
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

// Sensör pinleri (örnek)
const int TEMP_SENSOR_PIN = A0;
const int PRESSURE_SENSOR_PIN = A1;
const int HUMIDITY_SENSOR_PIN = A2;

// Telemetri deðiþkenleri
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
  
  // Analog pinleri giriþ olarak ayarla
  pinMode(TEMP_SENSOR_PIN, INPUT);
  pinMode(PRESSURE_SENSOR_PIN, INPUT);
  pinMode(HUMIDITY_SENSOR_PIN, INPUT);
  
  // Sensörleri baþlat
  initializeSensors();
  
  Serial.println("Gerçek Sensör Telemetri Sistemi Baþlatýldý");
  Serial.println("Paket boyutu: 96 byte");
  Serial.println("Gönderim aralýðý: 1 saniye");
  
  // Ýlk deðerleri ayarla
  telemetry.status = 1;
  telemetry.rocketLatitude = 39.925533; // Varsayýlan Ankara koordinatlarý
  telemetry.rocketLongitude = 32.866287;
  telemetry.payloadLatitude = 39.925533;
  telemetry.payloadLongitude = 32.866287;
  
  delay(2000);
}

void loop() {
  unsigned long currentTime = millis();
  
  if (currentTime - lastSendTime >= SEND_INTERVAL) {
    // Sensör verilerini oku
    readSensorData();
    
    // Paketi gönder
    sendTelemetryPacket();
    
    lastSendTime = currentTime;
    packetCounter++;
    
    // Debug çýktýsý
    printSensorData();
  }
  
  delay(10);
}

void initializeSensors() {
  // Gerçek sensör baþlatma kodlarý buraya gelecek
  
  /* MPU6050 örneði:
  mpu.initialize();
  if (mpu.testConnection()) {
    Serial.println("MPU6050 baðlantýsý baþarýlý");
  }
  */
  
  /* BMP280 örneði:
  if (bmp.begin()) {
    Serial.println("BMP280 baþlatýldý");
  }
  */
  
  Serial.println("Sensörler baþlatýldý (simülasyon modu)");
}

void readSensorData() {
  // 1. ÝVMEÖLÇER VE JÝROSKOP (MPU6050)
  readIMUData();
  
  // 2. BASINÇ VE SICAKLIK (BMP280)
  readPressureData();
  
  // 3. GPS VERÝLERÝ
  readGPSData();
  
  // 4. ÝRTÝFA HESAPLAMA
  calculateAltitude();
  
  // 5. HIZ HESAPLAMA
  calculateSpeed();
  
  // 6. NEM SENSÖRÜ
  readHumidityData();
  
  // 7. DURUM GÜNCELLEMESÝ
  updateStatus();
}

void readIMUData() {
  // Gerçek MPU6050 okuma kodu:
  /*
  int16_t ax, ay, az, gx, gy, gz;
  mpu.getMotion6(&ax, &ay, &az, &gx, &gy, &gz);
  
  telemetry.accelX = ax / 16384.0 * 9.81; // m/s²'ye çevir
  telemetry.accelY = ay / 16384.0 * 9.81;
  telemetry.accelZ = az / 16384.0 * 9.81;
  
  telemetry.gyroX = gx / 131.0; // derece/saniye
  telemetry.gyroY = gy / 131.0;
  telemetry.gyroZ = gz / 131.0;
  
  // Açý hesaplama
  telemetry.angle = atan2(telemetry.accelY, telemetry.accelZ) * 180 / PI;
  */
  
  // Simülasyon (gerçek sensör baðlanana kadar):
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
  // Gerçek BMP280 okuma kodu:
  /*
  telemetry.rocketPressure = bmp.readPressure() / 100.0; // hPa
  telemetry.rocketTemp = bmp.readTemperature(); // °C
  */
  
  // Analog sensör okuma örneði:
  int tempRaw = analogRead(TEMP_SENSOR_PIN);
  int pressureRaw = analogRead(PRESSURE_SENSOR_PIN);
  
  // Ham deðerleri gerçek deðerlere çevir (sensöre göre ayarlayýn)
  telemetry.rocketTemp = (tempRaw * 5.0 / 1024.0 - 0.5) * 100; // LM35 için
  telemetry.payloadTemp = telemetry.rocketTemp + random(-2, 3);
  
  // Basýnç simülasyonu (gerçek sensör baðlanana kadar)
  telemetry.rocketPressure = 1013.25 - (telemetry.rocketAltitude * 0.12);
  telemetry.payloadPressure = telemetry.rocketPressure + random(-5, 5);
}

void readGPSData() {
  // Gerçek GPS modülü okuma kodu:
  /*
  if (gps.location.isValid()) {
    telemetry.rocketLatitude = gps.location.lat();
    telemetry.rocketLongitude = gps.location.lng();
  }
  */
  
  // Simülasyon - hafif hareket
  telemetry.rocketLatitude += (random(-10, 10) / 1000000.0);
  telemetry.rocketLongitude += (random(-10, 10) / 1000000.0);
  telemetry.payloadLatitude = telemetry.rocketLatitude + (random(-20, 20) / 1000000.0);
  telemetry.payloadLongitude = telemetry.rocketLongitude + (random(-20, 20) / 1000000.0);
}

void calculateAltitude() {
  // Basýnç-irtifa dönüþümü
  float seaLevelPressure = 1013.25;
  telemetry.rocketAltitude = 44330 * (1.0 - pow(telemetry.rocketPressure / seaLevelPressure, 0.1903));
  
  // Payload irtifasý (roket irtifasýndan biraz farklý)
  telemetry.payloadAltitude = telemetry.rocketAltitude + random(-50, 20);
  
  // Negatif irtifa kontrolü
  if (telemetry.rocketAltitude < 0) telemetry.rocketAltitude = 0;
  if (telemetry.payloadAltitude < 0) telemetry.payloadAltitude = 0;
}

void calculateSpeed() {
  // Ývmeölçer verilerinden hýz hesaplama (basitleþtirilmiþ)
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
  // Gerçek nem sensörü okuma:
  /*
  int humidityRaw = analogRead(HUMIDITY_SENSOR_PIN);
  telemetry.payloadHumidity = map(humidityRaw, 0, 1023, 0, 100);
  */
  
  // Simülasyon
  telemetry.payloadHumidity = 45 + sin(millis() / 10000.0) * 20;
  telemetry.payloadHumidity = constrain(telemetry.payloadHumidity, 0, 100);
}

void updateStatus() {
  float missionTime = millis() / 1000.0;
  
  if (missionTime < 5) {
    telemetry.status = 0; // Baþlangýç
  } else if (telemetry.rocketAltitude > 10) {
    telemetry.status = 1; // Uçuþ
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
  
  // Tüm float deðerleri ekle
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
  
  // Gönder
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
  Serial.print("°C | Basýnç: ");
  Serial.print(telemetry.rocketPressure, 1);
  Serial.print("hPa | Durum: ");
  Serial.println(telemetry.status);
}

/*
  GERÇEK SENSÖR ENTEGRASYONU ÝÇÝN:
  
  1. Gerekli kütüphaneleri yükleyin:
     - MPU6050 için: MPU6050 library
     - BMP280 için: Adafruit BMP280 library
     - GPS için: TinyGPS++ library
  
  2. Sensörleri baðlayýn:
     - MPU6050: SDA->A4, SCL->A5 (I2C)
     - BMP280: SDA->A4, SCL->A5 (I2C)
     - GPS: RX->Pin2, TX->Pin3 (SoftwareSerial)
     - Analog sensörler: A0, A1, A2
  
  3. Simülasyon kodlarýný gerçek sensör okuma kodlarý ile deðiþtirin
  
  4. Kalibrasyon deðerlerini ayarlayýn
  
  Bu kod yapýsý gerçek sensörlerle kolayca entegre edilebilir!
*/