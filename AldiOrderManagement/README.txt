# AldiOrderManagement

## Beschreibung
Das Projekt dient als Webanwendung um die Bestellungen und Retouren von Aldi bearbeiten zu können.

Offene Bestellungen:
Hier werden alle Bestellungen mit dem Status 'IN_PROGRESS' aufgelistet. 
Mit dem Button "Export to XLS" wird eine XLS erzeugt, mit der die Bestellungen in das Warenwirtschaftssystem eingelesen werden können
In den Details der Bestellungen können diese noch storniert werden, wenn diese noch nicht exportiert wurden

Unterwegs
Hier werden alle Lieferungen mit dem Status 'SHIPPED' aufgelistet.
Diese sind hier solange, bis sie von den anderen Softwares verarbeitet wurden.
Unter den Details gibt es auch die Möglichkeit einer Stornierung.
Das ist zum Beispiel nötig, wenn ein Paket auf dem Weg verloren geht.

Storniert
Hier werden alle Bestellungen mit dem Status 'CANCELED' aufgelistet. 
Eine Bestellung wird als storniert gesehen, wenn alle Lieferungen den Status 'CANCELED' haben.
Hat eine oder mehrere Lieferungen den Status 'DELIVERED', gilt die Bestellung als geliefert.

Geliefert
Hier werden alle Bestellungen mit dem Status 'DELIVERED' aufgelistet. 
In den Details können Retouren erstellt werden.

Offene Retouren
Hier werden alle offenen Retouren mit dem Status 'IN_PROGRESS' aufgelistet.
Das sind entweder die manuell erzeugten oder vom Kunden selbst im Onlineshop erzeugten Retouren.
In den Details können der Carrier und die Trackingnummer für den Rückversand an die Aldi API übermittelt werden.
Der Status der Retoure wird dann auf 'RECEIVING' in der Datenbank aktualisiert.
Die Sendung des Rücksendelabels an den Kunden muss manuell über den Lieferanten erfolgen

Erwartete Retouren
Hier werden alle Retouren mit dem Status 'RECEIVING' aufgelistet.
Mit dem Button "PDF generieren" wird ein PDF mit allen Retoureninformationen erstellt.
Das hilft in der Produktion, eingehende Pakete leichter zu identifizieren.
In den Details können die Lieferungen als erhalten markiert werden.
Diese werden dann in der Datenbank auf 'RECEIVED' aktualisiert.
Sind alle Lieferungen als erhalten markiert, kann die gesamte Retoure akzeptiert oder abgelehnt werden.
In der Datenbank wird der Status dann auf 'COMPLETED' aktualisiert.

Abgeschlossene Retouren
Hier werden alle Retouren mit dem Status 'COMPLETED' aufgelistet. 

Statistik
Hier wird eine Übersicht aller verkauften Artikel dynamisch erstellt.
Das umfasst die Menge an verkauften Artikeln, Retouren und Retourenquote.