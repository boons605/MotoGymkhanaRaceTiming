class TimeSpan {
    constructor(microseconds) {
        this.microseconds = microseconds;
    }

    // Returns the integer number of minutes in this timespan
    GetMinutes() {
        return Math.floor(this.microseconds / 60000000);
    }

    // Returns the integer number of remaining seconds in this timespan (so if the timespan is 2 minutes and 19 seconds ling this method returns 19)
    GetSeconds() {
        var remainder = this.microseconds % 60000000;
        return Math.floor(remainder / 1000000);
    }

    // Returns the integer number of miliseconds that remains after minutes and seconds
    GetMilliseconds() {
        var remainder = this.microseconds % 1000000;

        return Math.floor(remainder / 1000);
    }
}