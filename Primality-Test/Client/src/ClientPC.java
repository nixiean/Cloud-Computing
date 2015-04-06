import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.PrintWriter;
import java.util.ArrayList;
import java.util.Collections;

import org.apache.http.HttpResponse;
import org.apache.http.client.HttpClient;
import org.apache.http.client.methods.HttpGet;
import org.apache.http.impl.client.HttpClientBuilder;

public class ClientPC {

	static String serverURL;
	static String username;
	static String netid;
	static String password;
	static long startNumber;
	static long endNumber;
	static ArrayList<Double> latencies;
	static HttpClient httpClient;
	static String outputFile = "runStatistics.txt";
	static StringBuilder runStats = new StringBuilder();

	public static void init(String serverURL, String username, String netid,
			String password, String startNumber, String endNumber) {
		ClientPC.serverURL = serverURL;
		ClientPC.username = username;
		ClientPC.netid = netid;
		ClientPC.password = password;
		ClientPC.startNumber = Long.parseLong(startNumber);
		ClientPC.endNumber = Long.parseLong(endNumber);
		ClientPC.latencies = new ArrayList<Double>();
		ClientPC.httpClient = HttpClientBuilder.create().build();
	}

	public static void getResponse(String url) {
		StringBuffer urlResponse = new StringBuffer();
		try {

			HttpGet request = new HttpGet(url);
			HttpResponse response = httpClient.execute(request);

			// Get the response
			BufferedReader rd = new BufferedReader(new InputStreamReader(
					response.getEntity().getContent()));

			String line = "";
			while ((line = rd.readLine()) != null) {
				urlResponse.append("\n" + line);
			}

			System.out.println(urlResponse);

		} catch (Exception e) {
			e.printStackTrace();
			System.out.println("Exception from Server.");
		}

	}

	public static boolean register() {
		//Generate and send the url for registration 
		String regURL = serverURL + "/registration?username=" + username
				+ "&netid=" + netid + "&password=" + password;
		getResponse(regURL);
		return true;
	}

	public static boolean login() {
		//Generate and send the url for login
		String loginURL = serverURL + "/login?username=" + username
				+ "&password=" + password;
		getResponse(loginURL);
		return true;
	}

	public static boolean isPrime() {
		//Generate and send the url for checking if the numner is prime
		if (startNumber % 2 == 0)
			startNumber++;

		for (long number = startNumber; number <= endNumber; number += 2) {
			String isPrimeURL = serverURL + "/primeChecker?inputNumber="
					+ number;
			long startTime = System.nanoTime();
			getResponse(isPrimeURL);
			latencies.add((double) (System.nanoTime() - startTime) / 1000000);
		}

		return true;
	}

	public static boolean logout() {
		//Generate and send the url for logout
		getResponse(serverURL + "/logout?");
		return true;
	}

	public static void runStatistics() {

		double min = Double.MAX_VALUE, max = Double.MIN_VALUE, mean = 0.0, median = 0.0;

		try {
			// Calculate Median
			Collections.sort(latencies);
			if (latencies.size() % 2 == 0)
				median = ((double) latencies.get(latencies.size() / 2) + (double) latencies
						.get(latencies.size() / 2 - 1)) / 2;
			else
				median = (double) latencies.get(latencies.size() / 2);

			// Find min, max
			min = latencies.get(0);
			max = latencies.get(latencies.size() - 1);

			// Calculate Mean
			for (double latency : latencies) {
				mean += latency;
			}
			mean = mean / latencies.size();

			runStats.append("RUN STATISTICS" + "\n");
			runStats.append(startNumber + " " + endNumber + "\n");
			runStats.append("Mean = " + mean + " ms" + "\n");
			runStats.append("Median = " + median + " ms" + "\n");
			runStats.append("Min = " + min + " ms" + "\n");
			runStats.append("Max = " + max + " ms" + "\n");

			writeToFile();

			System.out.println("RUN STATISTICS");
			System.out.println(startNumber + " " + endNumber);
			System.out.println("Mean = " + mean + " ms");
			System.out.println("Median = " + median + " ms");
			System.out.println("Min = " + min + " ms");
			System.out.println("Max = " + max + " ms");
		} catch (Exception e) {
			System.out.println("Error in calculating run statistics");
		}

	}

	public static void writeToFile() {

		while (true) {
			try (PrintWriter outStats = new PrintWriter(new BufferedWriter(
					new FileWriter(outputFile, true)))) {
				outStats.println(runStats);
			} catch (IOException e) {
				System.out.println("Error in writing to file");
				continue;
			}
			System.out.println("Run statistics output to file");
			break;
		}
	}

	public static void main(String[] args) {

		init(args[0], args[1], args[2], args[3], args[4], args[5]);

		// Register with the given user details 
		register();

		// Login with the details 
		login();

		// Check numbers for prime 
		isPrime();

		// Log out 
		logout();

		// Print Statistics
		runStatistics();

	}

}
