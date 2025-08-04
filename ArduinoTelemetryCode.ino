/*
  Arduino Telemetri Veri G�nderici - Debug Mesajs�z Versiyon
  
  Bu kod, C# uygulaman�z�n SerialPortService s�n�f�n�n bekledi�i formatta
  telemetri verilerini byte �eklinde g�nderir. Veriler grafikler sayfas�nda
  g�r�nt�lenecektir.
  
  �NEML�: Debug mesajlar� kapat�lm��t�r. Sadece binary veri g�nderilir.
  
  Paket Format� (96 byte):
  - Header: 0xAA, 0xBB, 0xCC, 0xDD (4 byte)
  - Packet Counter: 1 byte
  - Roket Altitude: 4 byte (float)
  - Roket Latitude: 4 byte (float)
  - Roket Longitude: 4 byte (float)
  - Payload Altitude: 4 byte (float)
  - Payload Latitude: 4 byte (float)
  - Payload Longitude: 4 byte (float)
  - Gyro X,Y,Z: 12 byte (3x float)
  - Accel X,Y,Z: 12 byte (3x float)
  - Angle: 4 byte (float)
  - Roket Temperature: 4 byte (float)
  - Payload Temperature: 4 byte (float)
  - Roket Pressure: 4 byte (float)
  - Payload Pressure: 4 byte (float)
  - Payload Humidity: 4 byte (float)
  - Roket Speed: 4 byte (float)
  - Payload Speed: 4 byte (float)
  - Status: 1 byte
  - CRC: 1 byte
  - Padding: 9 byte (toplam 96 byte i�in)
*/

#include <Wire.h>

// Sabitler
const byte PACKET_HEADER[] = {0xAA, 0xBB, 0xCC, 0xDD};
const int PACKET_SIZE = 96;
const unsigned long SEND_INTERVAL = 1000; // 1 saniye
const byte TEAM_ID = 255;

// DEBUG KONTROL: false yaparak debug mesajlar�n� kapatabilirsiniz
const bool ENABLE_DEBUG = false; // FALSE YAPARAK DEBUG MESAJLARINI KAPATIN!

// Telemetri de�i�kenleri
byte packetCounter = 0;
unsigned long lastSendTime = 0;

// Sim�lasyon de�i�kenleri
float rocketAltitude = 0.0;
float payloadAltitude = 0.0;
float missionTime = 0.0;
bool ascendingPhase = true;

// GPS koordinatlar� (Ankara �rnek konumu)
float rocketLatitude = 39.925533;
float rocketLongitude = 32.866287;
float payloadLatitude = 39.925533;
float payloadLongitude = 32.866287;

// Sens�r verileri
float gyroX = 0.0, gyroY = 0.0, gyroZ = 0.0;
float accelX = 0.0, accelY = 0.0, accelZ = 9.81;
float angle = 0.0;
float rocketTemp = 25.0, payloadTemp = 25.0;
float rocketPressure = 1013.25, payloadPressure = 1013.25;
float payloadHumidity = 45.0;
float rocketSpeed = 0.0, payloadSpeed = 0.0;
byte status = 1; // 1=Aktif, 0=Pasif

void setup() {
  Serial.begin(115200);
  
  // Sadece ba�lang�� mesaj� (debug kapal�ysa g�r�nmez)
  if (ENABLE_DEBUG) {
    Serial.println("Arduino Telemetri Sistemi Ba�lat�ld�");
    Serial.println("Paket boyutu: 96 byte");
    Serial.println("G�nderim aral���: 1 saniye");
    Serial.println("Header: 0xAA 0xBB 0xCC 0xDD");
    Serial.println("-------------------------");
  }
  
  delay(2000); // �lk g�nderimden �nce bekle
}

void loop() {
  unsigned long currentTime = millis();
  
  if (currentTime - lastSendTime >= SEND_INTERVAL) {
    // Telemetri verilerini g�ncelle
    updateTelemetryData();
    
    // Paketi olu�tur ve g�nder
    sendTelemetryPacket();
    
    lastSendTime = currentTime;
    packetCounter++;
    
    // Debug bilgisi (sadece debug a��ksa)
    if (ENABLE_DEBUG) {
      printDebugInfo();
    }
  }
  
  delay(10); // CPU y�k�n� azalt
}

void updateTelemetryData() {
  // Misyon zaman�n� g�ncelle (saniye cinsinden)
  missionTime = millis() / 1000.0;
  
  // Roket irtifas� sim�lasyonu (parabolik u�u�)
  if (missionTime < 30) {
    // �lk 30 saniye y�kselme
    rocketAltitude = missionTime * missionTime * 2.0; // H�zlanan y�kselme
    rocketSpeed = missionTime * 4.0;
    ascendingPhase = true;
  } else if (missionTime < 60) {
    // 30-60 saniye aras� tepe noktas� ve d��me
    float t = missionTime - 30;
    rocketAltitude = 1800 - (t * t * 1.5); // D��me
    rocketSpeed = -t * 3.0;
    ascendingPhase = false;
  } else {
    // 60 saniye sonras� yere yak�n
    rocketAltitude = max(0.0, 50.0 - (missionTime - 60) * 2.0);
    rocketSpeed = -10.0;
  }
  
  // Payload irtifas� (roket irtifas�ndan 10-50m d���k)
  payloadAltitude = rocketAltitude - random(10, 50);
  payloadSpeed = rocketSpeed + random(-5, 5);
  
  // GPS koordinatlar� (hafif hareket sim�lasyonu)
  rocketLatitude += (random(-10, 10) / 1000000.0); // Mikro de�i�imler
  rocketLongitude += (random(-10, 10) / 1000000.0);
  payloadLatitude += (random(-15, 15) / 1000000.0);
  payloadLongitude += (random(-15, 15) / 1000000.0);
  
  // Jiroskop verileri (derece/saniye)
  gyroX = sin(missionTime * 0.1) * 10.0 + random(-2, 2);
  gyroY = cos(missionTime * 0.15) * 8.0 + random(-2, 2);
  gyroZ = sin(missionTime * 0.05) * 5.0 + random(-1, 1);
  
  // �vme�l�er verileri (m/s�)
  accelX = sin(missionTime * 0.2) * 3.0 + random(-1, 1);
  accelY = cos(missionTime * 0.25) * 2.0 + random(-1, 1);
  if (ascendingPhase) {
    accelZ = 9.81 + random(5, 15); // Y�kselme s�ras�nda ekstra ivme
  } else {
    accelZ = 9.81 + random(-3, 3); // D��me s�ras�nda normal �ekim
  }
  
  // A�� bilgisi (derece)
  angle = sin(missionTime * 0.08) * 45.0;
  
  // S�cakl�k verileri (�C)
  float tempVariation = sin(missionTime * 0.02) * 5.0;
  rocketTemp = 25.0 + tempVariation + (rocketAltitude / 100.0 * -0.65); // Y�kseklik ile so�uma
  payloadTemp = rocketTemp + random(-2, 2);
  
  // Bas�n� verileri (hPa)
  rocketPressure = 1013.25 * exp(-0.00012 * rocketAltitude); // �rtifa ile bas�n� d���m�
  payloadPressure = rocketPressure + random(-5, 5);
  
  // Nem (sadece payload i�in, %)
  payloadHumidity = 45.0 + sin(missionTime * 0.03) * 20.0 + random(-5, 5);
  payloadHumidity = constrain(payloadHumidity, 0, 100);
  
  // Status g�ncelleme
  if (missionTime < 5) status = 0; // Ba�lang��
  else if (missionTime < 65) status = 1; // Aktif u�u�
  else status = 2; // �ni�/bitmi�
}

void sendTelemetryPacket() {
  byte packet[PACKET_SIZE];
  int index = 0;
  
  // Header (4 byte)
  for (int i = 0; i < 4; i++) {
    packet[index++] = PACKET_HEADER[i];
  }
  
  // Packet Counter (1 byte)
  packet[index++] = packetCounter;
  
  // Float de�erleri byte array'e �evir ve ekle
  addFloatToPacket(packet, index, rocketAltitude);
  addFloatToPacket(packet, index, rocketLatitude);
  addFloatToPacket(packet, index, rocketLongitude);
  addFloatToPacket(packet, index, payloadAltitude);
  addFloatToPacket(packet, index, payloadLatitude);
  addFloatToPacket(packet, index, payloadLongitude);
  
  addFloatToPacket(packet, index, gyroX);
  addFloatToPacket(packet, index, gyroY);
  addFloatToPacket(packet, index, gyroZ);
  
  addFloatToPacket(packet, index, accelX);
  addFloatToPacket(packet, index, accelY);
  addFloatToPacket(packet, index, accelZ);
  
  addFloatToPacket(packet, index, angle);
  addFloatToPacket(packet, index, rocketTemp);
  addFloatToPacket(packet, index, payloadTemp);
  addFloatToPacket(packet, index, rocketPressure);
  addFloatToPacket(packet, index, payloadPressure);
  addFloatToPacket(packet, index, payloadHumidity);
  addFloatToPacket(packet, index, rocketSpeed);
  addFloatToPacket(packet, index, payloadSpeed);
  
  // Status (1 byte)
  packet[index++] = status;
  
  // CRC hesapla (4'ten ba�layarak 82 byte i�in)
  byte crc = calculateCRC(packet, 4, 82);
  packet[index++] = crc;
  
  // Kalan yeri padding ile doldur (96 byte'a tamamla)
  while (index < PACKET_SIZE) {
    packet[index++] = 0x00;
  }
  
  // Paketi g�nder - �NEML�: Tek seferde t�m paketi g�nder
  Serial.write(packet, PACKET_SIZE);
  Serial.flush(); // G�nderimin tamamlanmas�n� bekle
}

void addFloatToPacket(byte* packet, int& index, float value) {
  // Float'� 4 byte olarak pakete ekle (little-endian)
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
  // C# kodundaki CalculateSimpleCRC ile uyumlu XOR tabanl� CRC
  byte crc = 0;
  for (int i = offset; i < offset + length; i++) {
    crc ^= data[i];
  }
  return crc;
}

void printDebugInfo() {
  // Bu fonksiyon sadece ENABLE_DEBUG true oldu�unda �a�r�l�r
  Serial.print("Debug - Paket #");
  Serial.print(packetCounter);
  Serial.print(" | Zaman: ");
  Serial.print(missionTime, 1);
  Serial.print("s | Roket Alt: ");
  Serial.print(rocketAltitude, 1);
  Serial.print("m | Payload Alt: ");
  Serial.print(payloadAltitude, 1);
  Serial.print("m | Status: ");
  Serial.println(status);
}

/*
  KULLANIM TAL�MATLARI:
  
  1. ENABLE_DEBUG de�i�kenini FALSE yap�n (debug mesajlar�n� kapat�r)
  2. Bu kodu Arduino'nuza y�kleyin
  3. Arduino'yu bilgisayar�n�za USB ile ba�lay�n
  4. C# uygulaman�zda COM5 portunu se�in
  5. Baud rate'i 115200 olarak ayarlay�n
  6. Ba�lant�y� ba�lat�n
  
  SORUN G�DERME ADIMLARI:
  
  1. E�er debug ��kt�s�n� g�rmek istiyorsan�z:
     - ENABLE_DEBUG = true; yap�n
     - Kodu y�kleyin
     - Serial Monitor'� a��n (C# uygulamas� kapal�yken!)
  
  2. C# uygulamas� i�in:
     - ENABLE_DEBUG = false; yap�n (�NEML�!)
     - Kodu y�kleyin
     - C# uygulamas�n� ba�lat�n
  
  DEBUG KAPALI �KEN BEKLEN?N:
  - Sadece binary paketler g�nderilir
  - Hi�bir text mesaj� yoktur
  - C# uygulamas� paketleri d�zg�n i�ler
  - Debug ��kt�s�nda "Normal telemetry packet processed" mesajlar� g�r�n�r
  
  VER� �ZELL�KLER�:
  - Roket 30 saniye y�kselir, sonra d��er
  - T�m sens�r verileri ger�ek�i de�erler i�erir
  - GPS koordinatlar� hafif hareket sim�lasyonu yapar
  - S�cakl�k ve bas�n� irtifa ile de�i�ir
  - CRC kontrol� tam uyumlu
*/