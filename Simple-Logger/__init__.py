import os, datetime

if os.path.isfile("logs/main.log"):
	if os.path.getsize("logs/main.log") >= 128000:
		ct = datetime.datetime.now()
		os.rename("logs/main.log", "logs/"+str(ct).replace(" ", "-")+".log")
else:
	if not os.path.exists("logs"):
		os.makedirs("logs")
	f = open("logs/main.log", "w").close()
