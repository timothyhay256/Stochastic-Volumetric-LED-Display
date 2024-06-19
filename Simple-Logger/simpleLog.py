import datetime, os
from inspect import getframeinfo, stack
i="" #Stop ide complaining

class bcolors:
	HEADER = '\033[95m'
	OKBLUE = '\033[94m'
	OKCYAN = '\033[96m'
	OKGREEN = '\033[92m'
	WARNING = '\033[93m'
	FAIL = '\033[91m'
	ENDC = '\033[0m'
	BOLD = '\033[1m'
	UNDERLINE = '\033[4m'

def log(message, level="i", thread="", detail=True, write=True):
	global i
	message = f"{bcolors.OKCYAN}{message}{bcolors.ENDC}"
	if thread != "":
		thread = f"{bcolors.OKGREEN}{thread}{bcolors.ENDC}"+":"
	if level.lower() == "i":
		i = f"{bcolors.HEADER}INFO:{bcolors.ENDC}"
		level = "INFO:"
	if level.lower() == "w":
		message = f"{bcolors.WARNING}{message.strip(bcolors.OKCYAN).strip(bcolors.ENDC)}{bcolors.ENDC}"
		i = f"{bcolors.WARNING}WARNING:{bcolors.ENDC}"
		level = "WARNING:"
	if level.lower() == "e":
		message = f"{bcolors.FAIL}{bcolors.BOLD}{message.strip(bcolors.OKCYAN).strip(bcolors.ENDC)}{bcolors.ENDC}"
		i = f"{bcolors.FAIL}{bcolors.BOLD}ERROR:{bcolors.ENDC}"
		level = "ERROR:"
	if detail:
		caller = getframeinfo(stack()[1][0])
		name = caller.filename.replace(os.getcwd()+"/", "")
		e = i + name +":"+f"{bcolors.OKGREEN}{str(caller.lineno)}{bcolors.ENDC}"+":"+thread+" "+message
		f = level+name+" "+str(caller.lineno)+":"+thread.strip(bcolors.OKGREEN).strip(bcolors.ENDC).strip(bcolors.WARNING).strip(bcolors.FAIL)+" "+message.strip(bcolors.OKCYAN).strip(bcolors.ENDC).strip(bcolors.WARNING).strip(bcolors.FAIL)
	else:
		e = i+thread+" "+message
		f = level+thread.strip(bcolors.OKGREEN).strip(bcolors.ENDC).strip(bcolors.WARNING).strip(bcolors.FAIL)+" "+message.strip(bcolors.OKCYAN).strip(bcolors.OKGREEN).strip(bcolors.ENDC).strip(bcolors.WARNING).strip(bcolors.FAIL)
	print(e)
	if write:
		logF = open("logs/main.log", "a")
		logF.write(str(datetime.datetime.now())+":"+f.replace(bcolors.ENDC, "")+"\n")
		logF.close()
