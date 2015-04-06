import java.io.IOException;
import java.io.PrintWriter;
import java.net.InetAddress;

import javax.servlet.ServletException;
import javax.servlet.annotation.WebServlet;
import javax.servlet.http.HttpServlet;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;
import javax.servlet.http.HttpSession;

/**
 * Servlet implementation class primeCheker
 */
@WebServlet("/primeChecker")
public class primeChecker extends HttpServlet {
	private static final long serialVersionUID = 1L;

	/**
	 * @see HttpServlet#HttpServlet()
	 */
	public primeChecker() {
		super();
		// TODO Auto-generated constructor stub
	}

	/**
	 * @see HttpServlet#doGet(HttpServletRequest request, HttpServletResponse
	 *      response)
	 */
	protected void doGet(HttpServletRequest request,
			HttpServletResponse response) throws ServletException, IOException {
		// TODO Auto-generated method stub
		PrintWriter out = response.getWriter();
		try {
			if (DBUtilities.authenticateUser(request.getCookies())) {
				//User is logged in
				long number = Long.parseLong(request
						.getParameter("inputNumber"));
				if (PrimeCheck.isPrime(number)) {
					out.println(number + " is prime." + " Executed from "
							+ InetAddress.getLocalHost().getHostAddress()
							+ " <a href='prime.jsp'>Try another number.</a>");
				} else {
					out.println(number + " is not prime." + " Executed from "
							+ InetAddress.getLocalHost().getHostAddress()
							+ " <a href='prime.jsp'>Try another number.</a>");
				}

			} else {
				//User is not logged in
				out.println("User not logged in. <a href='index.jsp'>Please Login.</a>");
			}
		} catch (Exception e) {
			e.printStackTrace();
			out.println("Error from RDS while authenticating. <a href='index.jsp'>Please try again.</a>");
		}

	}

	/**
	 * @see HttpServlet#doPost(HttpServletRequest request, HttpServletResponse
	 *      response)
	 */
	protected void doPost(HttpServletRequest request,
			HttpServletResponse response) throws ServletException, IOException {
		// TODO Auto-generated method stub
		doGet(request, response);
	}

}
