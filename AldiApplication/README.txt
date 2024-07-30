# AldiApplication

## Beschreibung
Das Projekt hat 3 Kernfunktionen:

1. Es werden neue Bestellungen von der Aldi API abgerufen und als InProgress gemeldet.
   Diese werden dann in der lokalen Datenbank gespeichert.
   
2. Das Warenwirtschafssystem erstellt eine Exceldatei mit allen versendeten Lieferungen.
   Diese wird von dieser Software ausgelesen und die Lieferungen der Aldi API übermittelt.
   Diese werden dann auch in der lokalen Datenbank gespeichert.
   
3. Es werden neue Retouren abgerufen, die über den Onlineshop vom Kunden erstellt wurden.
   Diese werden auch als InProgress der API übermittelt und in der lokalen Datenbank gespeichert