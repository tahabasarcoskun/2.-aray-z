/*
  Arduino Telemetri Veri Gönderici - Debug Mesajsýz Versiyon
  
  Bu kod, C# uygulamanýzýn SerialPortService sýnýfýnýn beklediði formatta
  telemetri verilerini byte þeklinde gönderir. Veriler grafikler sayfasýnda
  görüntülenecektir.
  
  ÖNEMLÝ: Debug mesajlarý kapatýlmýþtýr. Sadece binary veri gönderilir.
  
  Paket Formatý (96 byte):
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
  - Padding: 9 byte (toplam 96 byte için)
*/

#include <Wire.h>

// Sabitler
const byte PACKET_HEADER[] = {0xAA, 0xBB, 0xCC, 0xDD};
const int PACKET_SIZE = 96;
const unsigned long SEND_INTERVAL = 1000; // 1 saniye
const byte TEAM_ID = 255;

// DEBUG KONTROL: false yaparak debug mesajlarýný kapatabilirsiniz
const bool ENABLE_DEBUG = false; // FALSE YAPARAK DEBUG MESAJLARINI KAPATIN!

// Telemetri deðiþkenleri
byte packetCounter = 0;
unsigned long lastSendTime = 0;

// Simülasyon deðiþkenleri
float rocketAltitude = 0.0;
float payloadAltitude = 0.0;
float missionTime = 0.0;
bool ascendingPhase = true;

// GPS koordinatlarý (Ankara örnek konumu)
float rocketLatitude = 39.925533;
float rocketLongitude = 32.866287;
float payloadLatitude = 39.925533;
float payloadLongitude = 32.866287;

// Sensör verileri
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
  
  // Sadece baþlangýç mesajý (debug kapalýysa görünmez)
  if (ENABLE_DEBUG) {
    Serial.println("Arduino Telemetri Sistemi Baþlatýldý");
    Serial.println("Paket boyutu: 96 byte");
    Serial.println("Gönderim aralýðý: 1 saniye");
    Serial.println("Header: 0xAA 0xBB 0xCC 0xDD");
    Serial.println("-------------------------");
  }
  
  delay(2000); // Ýlk gönderimden önce bekle
}

void loop() {
  unsigned long currentTime = millis();
  
  if (currentTime - lastSendTime >= SEND_INTERVAL) {
    // Telemetri verilerini güncelle
    updateTelemetryData();
    
    // Paketi oluþtur ve gönder
    sendTelemetryPacket();
    
    lastSendTime = currentTime;
    packetCounter++;
    
    // Debug bilgisi (sadece debug açýksa)
    if (ENABLE_DEBUG) {
      printDebugInfo();
    }
  }
  
  delay(10); // CPU yükünü azalt
}

void updateTelemetryData() {
  // Misyon zamanýný güncelle (saniye cinsinden)
  missionTime = millis() / 1000.0;
  
  // Roket irtifasý simülasyonu (parabolik uçuþ)
  if (missionTime < 30) {
    // Ýlk 30 saniye yükselme
    rocketAltitude = missionTime * missionTime * 2.0; // Hýzlanan yükselme
    rocketSpeed = missionTime * 4.0;
    ascendingPhase = true;
  } else if (missionTime < 60) {
    // 30-60 saniye arasý tepe noktasý ve düþme
    float t = missionTime - 30;
    rocketAltitude = 1800 - (t * t * 1.5); // Düþme
    rocketSpeed = -t * 3.0;
    ascendingPhase = false;
  } else {
    // 60 saniye sonrasý yere yakýn
    rocketAltitude = max(0.0, 50.0 - (missionTime - 60) * 2.0);
    rocketSpeed = -10.0;
  }
  
  // Payload irtifasý (roket irtifasýndan 10-50m düþük)
  payloadAltitude = rocketAltitude - random(10, 50);
  payloadSpeed = rocketSpeed + random(-5, 5);
  
  // GPS koordinatlarý (hafif hareket simülasyonu)
  rocketLatitude += (random(-10, 10) / 1000000.0); // Mikro deðiþimler
  rocketLongitude += (random(-10, 10) / 1000000.0);
  payloadLatitude += (random(-15, 15) / 1000000.0);
  payloadLongitude += (random(-15, 15) / 1000000.0);
  
  // Jiroskop verileri (derece/saniye)
  gyroX = sin(missionTime * 0.1) * 10.0 + random(-2, 2);
  gyroY = cos(missionTime * 0.15) * 8.0 + random(-2, 2);
  gyroZ = sin(missionTime * 0.05) * 5.0 + random(-1, 1);
  
  // Ývmeölçer verileri (m/s²)
  accelX = sin(missionTime * 0.2) * 3.0 + random(-1, 1);
  accelY = cos(missionTime * 0.25) * 2.0 + random(-1, 1);
  if (ascendingPhase) {
    accelZ = 9.81 + random(5, 15); // Yükselme sýrasýnda ekstra ivme
  } else {
    accelZ = 9.81 + random(-3, 3); // Düþme sýrasýnda normal çekim
  }
  
  // Açý bilgisi (derece)
  angle = sin(missionTime * 0.08) * 45.0;
  
  // Sýcaklýk verileri (°C)
  float tempVariation = sin(missionTime * 0.02) * 5.0;
  rocketTemp = 25.0 + tempVariation + (rocketAltitude / 100.0 * -0.65); // Yükseklik ile soðuma
  payloadTemp = rocketTemp + random(-2, 2);
  
  // Basýnç verileri (hPa)
  rocketPressure = 1013.25 * exp(-0.00012 * rocketAltitude); // Ýrtifa ile basýnç düþümü
  payloadPressure = rocketPressure + random(-5, 5);
  
  // Nem (sadece payload için, %)
  payloadHumidity = 45.0 + sin(missionTime * 0.03) * 20.0 + random(-5, 5);
  payloadHumidity = constrain(payloadHumidity, 0, 100);
  
  // Status güncelleme
  if (missionTime < 5) status = 0; // Baþlangýç
  else if (missionTime < 65) status = 1; // Aktif uçuþ
  else status = 2; // Ýniþ/bitmiþ
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
  
  // Float deðerleri byte array'e çevir ve ekle
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
  
  // CRC hesapla (4'ten baþlayarak 82 byte için)
  byte crc = calculateCRC(packet, 4, 82);
  packet[index++] = crc;
  
  // Kalan yeri padding ile doldur (96 byte'a tamamla)
  while (index < PACKET_SIZE) {
    packet[index++] = 0x00;
  }
  
  // Paketi gönder - ÖNEMLÝ: Tek seferde tüm paketi gönder
  Serial.write(packet, PACKET_SIZE);
  Serial.flush(); // Gönderimin tamamlanmasýný bekle
}

void addFloatToPacket(byte* packet, int& index, float value) {
  // Float'ý 4 byte olarak pakete ekle (little-endian)
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
  // C# kodundaki CalculateSimpleCRC ile uyumlu XOR tabanlý CRC
  byte crc = 0;
  for (int i = offset; i < offset + length; i++) {
    crc ^= data[i];
  }
  return crc;
}

void printDebugInfo() {
  // Bu fonksiyon sadece ENABLE_DEBUG true olduðunda çaðrýlýr
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
  KULLANIM TALÝMATLARI:
  
  1. ENABLE_DEBUG deðiþkenini FALSE yapýn (debug mesajlarýný kapatýr)
  2. Bu kodu Arduino'nuza yükleyin
  3. Arduino'yu bilgisayarýnýza USB ile baðlayýn
  4. C# uygulamanýzda COM5 portunu seçin
  5. Baud rate'i 115200 olarak ayarlayýn
  6. Baðlantýyý baþlatýn
  
  SORUN GÝDERME ADIMLARI:
  
  1. Eðer debug çýktýsýný görmek istiyorsanýz:
     - ENABLE_DEBUG = true; yapýn
     - Kodu yükleyin
     - Serial Monitor'ü açýn (C# uygulamasý kapalýyken!)
  
  2. C# uygulamasý için:
     - ENABLE_DEBUG = false; yapýn (ÖNEMLÝ!)
     - Kodu yükleyin
     - C# uygulamasýný baþlatýn
  
  DEBUG KAPALI ÝKEN BEKLEN?N:
  - Sadece binary paketler gönderilir
  - Hiçbir text mesajý yoktur
  - C# uygulamasý paketleri düzgün iþler
  - Debug çýktýsýnda "Normal telemetry packet processed" mesajlarý görünür
  
  VERÝ ÖZELLÝKLERÝ:
  - Roket 30 saniye yükselir, sonra düþer
  - Tüm sensör verileri gerçekçi deðerler içerir
  - GPS koordinatlarý hafif hareket simülasyonu yapar
  - Sýcaklýk ve basýnç irtifa ile deðiþir
  - CRC kontrolü tam uyumlu
*/