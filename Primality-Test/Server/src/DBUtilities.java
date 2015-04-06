import java.sql.DriverManager;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.HashSet;
import java.util.UUID;

import javax.servlet.http.Cookie;

import com.mysql.jdbc.Connection;
import com.mysql.jdbc.Statement;

public class DBUtilities {

	static HashSet<String> userSessions = new HashSet<String>();

	/*
	 * Get the connection to RDS
	 */
	public static Statement connectDB() {
		String url = "jdbc:mysql://cs5412-primechecker.cywg49njkxjp.us-west-2.rds.amazonaws.com:3306/CS5412_PrimeChecker";
		String username = "primechecker";
		String password = "primechecker";

		try {
			Class.forName("com.mysql.jdbc.Driver");
			Connection conn = (Connection) DriverManager.getConnection(url,
					username, password);
			Statement stmt = (Statement) conn.createStatement();
			return stmt;
		} catch (Exception e) {
			return null;
		}

	}

	/*
	 * Check if the user is registered by making a call to RDS
	 */
	public static boolean isRegistered(String username, String password)
			throws Exception {

		Statement stmt = DBUtilities.connectDB();

		String query = "select * from users where username='" + username
				+ "' and password='" + password + "';";

		ResultSet res;
		res = stmt.executeQuery(query);
		if (res.next()) {

			query = "UPDATE users SET session = 'Y' WHERE username = '"
					+ username + "';";
			stmt.executeUpdate(query);
			return true;
		} else {
			return false;
		}
	}

	/*
	 * Register the user by storing details in RDS 
	 */
	public static boolean registerUser(String username, String netid,
			String password) throws Exception {

		Statement stmt = DBUtilities.connectDB();

		String query = "INSERT INTO users VALUES ('" + username + "','" + netid
				+ "','" + password + "','N','NULL');";

		int returnStatus = stmt.executeUpdate(query);
		if (returnStatus > 0) {
			return true;
		} else {
			return false;
		}

	}

	/*
	 * Check if a session is valid for user
	 */
	public static boolean isLoggedIn(String sessionID) throws Exception {

		Statement stmt = DBUtilities.connectDB();

		String query = "SELECT username FROM users WHERE session = 'Y' AND sessionID = '"
				+ sessionID + "';";

		ResultSet res = stmt.executeQuery(query);

		if (res.first()) {
			userSessions.add(sessionID);
			return true;
		}
		return false;
	}

	/*
	 * Invalidate the user's session from RDS
	 */
	public static void logOutUser(String sessionID) throws Exception {
		Statement stmt = DBUtilities.connectDB();
		String queryDB = "UPDATE users SET session = 'N' WHERE sessionID = '"
				+ sessionID + "';";
		int res = stmt.executeUpdate(queryDB);
		userSessions.remove(sessionID);
	}

	/*
	 * Generate the session ID for user (first time after logging in) 
	 */
	public static String generateSessionID(String username) throws SQLException {
		// TODO Auto-generated method stub
		String sessionID = UUID.randomUUID().toString();
		Statement stmt = DBUtilities.connectDB();
		String queryDB = "UPDATE users SET sessionID = '"
				+ sessionID + "' WHERE username = '" + username + "';";
		int res = stmt.executeUpdate(queryDB);
		userSessions.add(sessionID);
		return sessionID;
	}
	
	/*
	 * Check if user's authentication
	 */
	public static boolean authenticateUser(Cookie[] cookies) throws Exception {
		// TODO Auto-generated method stub
		String sessionID = getSessionID(cookies); 
		if (sessionID == null) {
			return false;
		} else {
			if (userSessions.contains(sessionID)) {
				return true;
			} else {
				return isLoggedIn(sessionID);
			}
		}

	}

	/*
	 * Get the session ID from the cookie
	 */
	public static String getSessionID(Cookie[] cookies) {
		String sessionID = null;
		for (Cookie cookie : cookies) {
			if ("JSessionID".equals(cookie.getName())) {
				sessionID = cookie.getValue();
			}
		}
		return sessionID;
	}

}
