from simpleLog import log
import os

os.remove("logs/main.log")
#Test 1
print("Everything should look like how you want it to.\n")
log("Testing...", "i")
log("I should have a thread listed.", "i", "otherThread")
log("Warning...", "w")
log("I should have a thread listed.", "w", "otherThread")
log("Error!", "e")
log("I should have a thread listed.", "e", "otherThread")
print("\nYou should stop seeing any details!\n")
log("Testing...", "i", "", False)
log("I should have a thread listed.", "i", "otherThread",False)
log("Warning...", "w", "", False)
log("I should have a thread listed.", "w", "otherThread", False)
log("Error!", "e", "", False)
log("I should have a thread listed.", "e", "otherThread", False)
print("\nYou should not see the following in the log file!\n")
log("I should not appear in the log file!", "i", "", False, False)
log("I should not appear in the log file!", "i", "", True, False)

print("\nMake sure the log file doesn't have the above or any color codes!")

